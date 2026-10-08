// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.JwtBearer;

/// <summary>
/// 授权策略执行程序
/// </summary>
internal sealed class AppAuthorizationHandler : IAuthorizationHandler, IAuthorizationMiddlewareResultHandler,
    IAsyncAlwaysRunResultFilter, IAsyncResourceFilter
{
    // 用于在同一请求中传递授权失败状态和响应数据，使用对象键避免与其他组件冲突。
    private static readonly object FailureResultKey = new();

    // 执行框架默认的授权结果处理，保留 Challenge、Forbid 及授权成功后继续请求的行为。
    private readonly AuthorizationMiddlewareResultHandler _defaultResultHandler = new();

    /// <inheritdoc />
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        // 同一个授权处理器可能由端点中间件、MVC 或 Hub 调用，三者提供的 Resource 类型不同。
        var filterContext = context.Resource as AuthorizationFilterContext;
        var hubInvocationContext = context.Resource as HubInvocationContext;
        HttpContext httpContext = filterContext?.HttpContext
                                  ?? context.Resource as HttpContext ?? hubInvocationContext?.Context.GetHttpContext();
        AppAuthorizeRequirement[] requirements = context.PendingRequirements.OfType<AppAuthorizeRequirement>().ToArray();
        // 与 HTTP 无关的第三方资源策略继续交给其自身处理器。
        if (httpContext == null)
        {
            if (requirements.Length > 0)
            {
                context.Fail();
            }

            return;
        }

        IJwtBearerHandle jwtBearerHandle = (hubInvocationContext?.ServiceProvider ?? httpContext.RequestServices)
            .GetService<IJwtBearerHandle>();
        if (!await JwtBearerUtil.AutoRefreshTokenAsync(context, httpContext))
        {
            context.Fail();
            if (hubInvocationContext == null && !httpContext.Response.HasStarted)
            {
                httpContext.Response.Headers.Remove("x-access-token");
                httpContext.SignOutToSwagger();
            }

            object body = jwtBearerHandle == null ? null : await jwtBearerHandle.AuthorizeFailHandle(context, httpContext, null);
            // 这里只保存状态和正文，不立即执行 JsonResult；最终响应由对应的 HTTP 管线输出。
            // 已建立的 Hub 连接使用 SignalR 协议拒绝调用，不再生成 HTTP 拒绝响应。
            if (hubInvocationContext == null)
            {
                httpContext.Items[FailureResultKey] = new JsonResult(body) {StatusCode = StatusCodes.Status401Unauthorized};
            }

            return;
        }

        if (jwtBearerHandle != null)
        {
            Exception authorizeException = null;
            bool authorized;
            try
            {
                authorized = await jwtBearerHandle.AuthorizeHandle(context, httpContext);
            }
            // 请求取消不是身份失效，不进入 AuthorizeFailHandle。
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                authorized = false;
                authorizeException = exception;
            }

            if (!authorized)
            {
                // 自定义响应不代替失败状态，其他处理器不能重新把本次拒绝变成成功。
                context.Fail();
                if (hubInvocationContext == null && !httpContext.Response.HasStarted)
                {
                    // 刷新后仍须通过应用身份检查，失败时不得交付新令牌。
                    httpContext.Response.Headers.Remove("access-token");
                    httpContext.Response.Headers.Remove("x-access-token");
                }

                object body = await jwtBearerHandle.AuthorizeFailHandle(context, httpContext, authorizeException);
                // context.Fail() 只表示授权失败，不能区分身份失效与权限不足，因此另外记录 401。
                if (hubInvocationContext == null)
                {
                    httpContext.Items[FailureResultKey] = new JsonResult(body) {StatusCode = StatusCodes.Status401Unauthorized};
                }

                return;
            }
        }

        // 身份已通过。后续 Fast、Roles 或 Claims 拒绝均为 403，不能被刷新前的认证结果误判为 401。
        // 这是后续拒绝时使用的默认状态，并不表示当前请求已被拒绝；授权成功后会清除它。
        if (hubInvocationContext == null)
        {
            httpContext.Items[FailureResultKey] = new JsonResult(null) {StatusCode = StatusCodes.Status403Forbidden};
        }

        bool skipFastPermission = httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowForbiddenAttribute>() != null;
        PermissionAttribute permissionMetadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<PermissionAttribute>();
        // 仅含 Roles/Claims 的组合策略不经过默认策略提供器；仍执行显式 Fast 权限钩子，
        // 但这一附加检查的成功不能替第三方要求调用 Succeed。
        if (requirements.Length == 0 && (jwtBearerHandle != null || permissionMetadata != null))
        {
            requirements = [new AppAuthorizeRequirement()];
        }

        foreach (AppAuthorizeRequirement requirement in requirements)
        {
            // 只确认 Fast 自己拥有的要求，绝不替角色、声明或第三方策略调用 Succeed。
            if (skipFastPermission || (jwtBearerHandle == null && requirement.Policies.Length == 0 && permissionMetadata == null))
            {
                if (context.Requirements.Contains(requirement))
                {
                    context.Succeed(requirement);
                }

                continue;
            }

            if (jwtBearerHandle == null)
            {
                context.Fail();
                continue;
            }

            Exception permissionException = null;
            bool permitted;
            try
            {
                permitted = await jwtBearerHandle.PermissionHandle(context, requirement, httpContext);
            }
            // 请求取消不是权限不足，不进入 PermissionFailHandle。
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                permitted = false;
                permissionException = exception;
            }

            if (permitted)
            {
                if (context.Requirements.Contains(requirement))
                {
                    context.Succeed(requirement);
                }

                continue;
            }

            context.Fail();
            object body = await jwtBearerHandle.PermissionFailHandle(context, requirement, httpContext, permissionException);
            if (hubInvocationContext == null)
            {
                httpContext.Items[FailureResultKey] = new JsonResult(body) {StatusCode = StatusCodes.Status403Forbidden};
            }
        }
    }

    /// <inheritdoc />
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // 端点授权中间件的最终出口：取出并移除本次记录，避免后续 MVC 管线复用旧结果。
        context.Items.Remove(FailureResultKey, out object value);
        // HasStarted 表示响应头已发送，不一定是正文已写完；此时不能再改状态或补写失败正文。
        if (context.Response.HasStarted)
        {
            return;
        }

        if (!authorizeResult.Succeeded && value is JsonResult failure)
        {
            // 在最终拒绝处选择 Challenge/Forbid，保留认证方案、事件及 Bearer Challenge 响应头。
            authorizeResult = failure.StatusCode == StatusCodes.Status401Unauthorized
                ? PolicyAuthorizationResult.Challenge()
                : PolicyAuthorizationResult.Forbid();
            // 先执行认证方案的 Challenge/Forbid，让框架设置状态、响应头并调用已注册的事件。
            await _defaultResultHandler.HandleAsync(next, context, policy, authorizeResult);
            // 事件可能已经写出响应或改变状态；只有它尚未接管响应时，才补充失败回调返回的正文。
            if (failure.Value != null && !context.Response.HasStarted && context.Response.StatusCode == failure.StatusCode)
            {
                await context.Response.WriteAsJsonAsync(failure.Value, context.RequestAborted);
            }

            return;
        }

        // 授权成功时由默认处理器调用 next；没有 Fast 失败记录时也沿用框架的原始结果。
        await _defaultResultHandler.HandleAsync(next, context, policy, authorizeResult);
    }


    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // MVC 的拒绝不经过上面的端点结果处理器；AlwaysRun 保证授权过滤器短路后仍能执行这里。
        context.HttpContext.Items.Remove(FailureResultKey, out object value);
        // 响应头已发送时不再替换结果，避免重复执行拒绝或写入第二份正文。
        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        if (value is JsonResult failure && (context.Result is ChallengeResult || context.Result is ForbidResult))
        {
            // AuthorizeFilter 会覆盖授权处理器中设置的 Result，因此在其完成后处理最终结果。
            // 保留原结果指定的认证方案和属性，只按记录修正 Challenge/Forbid 的选择。
            IList<string> schemes = context.Result is ChallengeResult challenge
                ? challenge.AuthenticationSchemes
                : ((ForbidResult)context.Result).AuthenticationSchemes;
            AuthenticationProperties properties = context.Result is ChallengeResult challengeResult
                ? challengeResult.Properties
                : ((ForbidResult)context.Result).Properties;
            IActionResult rejection = failure.StatusCode == StatusCodes.Status401Unauthorized
                ? new ChallengeResult(schemes, properties)
                : new ForbidResult(schemes, properties);
            context.Result = new FailureResult(rejection, failure);
        }

        await next();
    }


    /// <inheritdoc />
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        // 能进入资源过滤器说明 MVC 授权已通过，不能把 Action 主动返回的 Challenge/Forbid 当作授权失败。
        context.HttpContext.Items.Remove(FailureResultKey);
        await next();
    }

    // 将 MVC 的拒绝操作和自定义正文作为一个结果执行，避免直接返回 JSON 时跳过认证方案的事件。
    private sealed class FailureResult(IActionResult rejection, JsonResult failure) : ActionResult
    {
        public override async Task ExecuteResultAsync(ActionContext context)
        {
            // rejection 负责 Challenge/Forbid；failure 仅在响应尚未被事件接管时提供 JSON 正文。
            await rejection.ExecuteResultAsync(context);
            if (failure.Value != null
                && !context.HttpContext.Response.HasStarted
                && context.HttpContext.Response.StatusCode == failure.StatusCode)
            {
                await failure.ExecuteResultAsync(context);
            }
        }
    }
}
