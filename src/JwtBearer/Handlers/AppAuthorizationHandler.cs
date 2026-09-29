// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Fast.JwtBearer;

/// <summary>
/// 授权策略执行程序
/// </summary>
internal sealed class AppAuthorizationHandler : IAuthorizationHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
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
            httpContext.SignOutToSwagger();
            if (jwtBearerHandle != null)
            {
                SetMvcFailure(await jwtBearerHandle.AuthorizeFailHandle(context, httpContext, null),
                    StatusCodes.Status401Unauthorized);
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
            catch (Exception exception)
            {
                authorized = false;
                authorizeException = exception;
            }

            if (!authorized)
            {
                // 自定义响应不代替失败状态，其他处理器不能重新把本次拒绝变成成功。
                context.Fail();
                SetMvcFailure(await jwtBearerHandle.AuthorizeFailHandle(context, httpContext, authorizeException),
                    StatusCodes.Status401Unauthorized);
                return;
            }
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
            SetMvcFailure(await jwtBearerHandle.PermissionFailHandle(context, requirement, httpContext, permissionException),
                StatusCodes.Status403Forbidden);
        }

        void SetMvcFailure(object result, int statusCode)
        {
            // 非 MVC 资源不强行写 MVC Result；授权中间件和 SignalR 使用已记录的失败状态。
            if (filterContext != null && result != null)
            {
                filterContext.Result = new JsonResult(result) {StatusCode = statusCode};
            }
        }
    }
}
