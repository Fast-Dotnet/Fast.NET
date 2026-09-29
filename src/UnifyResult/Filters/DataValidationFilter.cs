// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Fast.UnifyResult;

/// <summary>
/// 数据验证拦截器
/// </summary>
internal sealed class DataValidationFilter : IAsyncActionFilter, IOrderedFilter
{
    /// <summary>
    /// API 行为配置选项
    /// </summary>
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="options">当前组件使用的选项</param>
    public DataValidationFilter(IOptions<ApiBehaviorOptions> options)
    {
        _apiBehaviorOptions = options.Value;
    }

    /// <summary>
    /// 过滤器排序
    /// </summary>
    private const int FilterOrder = -1000;

    /// <inheritdoc />
    public int Order => FilterOrder;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 排除 WebSocket 请求处理
        if (context.HttpContext.WebSockets.IsWebSocketRequest)
        {
            await next();
            return;
        }

        // 获取控制器/方法信息
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;

        // 跳过验证类型
        Type nonValidationAttributeType = typeof(NonValidationAttribute);
        MethodInfo method = actionDescriptor?.MethodInfo;

        // 获取验证状态
        ModelStateDictionary modelState = context.ModelState;

        // 如果参数数量为 0 或贴了 [NonValidation] 特性 或所在类型贴了 [NonValidation] 特性或验证成功或已经设置了结果，则跳过验证
        if (actionDescriptor?.Parameters.Count == 0
            || method?.IsDefined(nonValidationAttributeType, true) == true
            || method?.DeclaringType?.IsDefined(nonValidationAttributeType, true) == true
            || modelState.IsValid
            || method?.DeclaringType?.Assembly.GetName().Name?.StartsWith("Microsoft.AspNetCore.OData") == true
            || context.Result != null)
        {
            await CallUnHandleResult(context, next, actionDescriptor);
            return;
        }

        // 处理执行前验证信息
        bool handledResult = await HandleValidation(context, actionDescriptor, modelState);

        // 处理 Mvc 未处理结果情况
        if (!handledResult)
        {
            await CallUnHandleResult(context, next, actionDescriptor);
        }
    }

    /// <summary>
    /// 调用未处理的结果类型
    /// </summary>
    /// <param name="context">当前操作上下文</param>
    /// <param name="next">处理管道中的下一个委托</param>
    /// <param name="actionDescriptor">当前控制器操作的描述信息</param>
    /// <returns>表示异步调用未处理的结果类型的任务</returns>
    private async Task CallUnHandleResult(ActionExecutingContext context, ActionExecutionDelegate next,
        ControllerActionDescriptor actionDescriptor)
    {
        // 处理执行后验证信息
        ActionExecutedContext resultContext = await next();

        // 如果异常不为空且属于友好验证异常
        if (resultContext.Exception != null
            && resultContext.Exception is UserFriendlyException userFriendlyException
            && userFriendlyException.ValidationException)
        {
            // 存储验证执行结果
            context.HttpContext.Items[nameof(DataValidationFilter) + nameof(UserFriendlyException)] = resultContext;

            // 处理验证信息
            _ = await HandleValidation(context, actionDescriptor, userFriendlyException.ErrorMessage, resultContext,
                userFriendlyException);
        }
    }

    /// <summary>
    /// 内部处理异常
    /// </summary>
    /// <param name="context">当前操作上下文</param>
    /// <param name="actionDescriptor">当前控制器操作的描述信息</param>
    /// <param name="errors">模型验证产生的错误集合</param>
    /// <param name="resultContext">用于写入验证失败结果的过滤器上下文</param>
    /// <param name="userFriendlyException">根据验证错误构造的用户友好异常</param>
    /// <returns>返回 <see langword="false"/> 表示结果没有处理</returns>
    private async Task<bool> HandleValidation(ActionExecutingContext context, ControllerActionDescriptor actionDescriptor,
        object errors, ActionExecutedContext resultContext = null, UserFriendlyException userFriendlyException = null)
    {
        dynamic finalContext = resultContext != null ? resultContext : context;

        // 解析验证消息
        ValidationMetadata validationMetadata = UnifyContext.GetValidationMetadata(errors);
        validationMetadata.ErrorCode = userFriendlyException?.ErrorCode;
        validationMetadata.OriginErrorCode = userFriendlyException?.OriginErrorCode;
        validationMetadata.StatusCode = userFriendlyException?.StatusCode;
        validationMetadata.Data = userFriendlyException?.Data;

        // 存储验证信息
        context.HttpContext.Items[nameof(DataValidationFilter) + nameof(ValidationMetadata)] = validationMetadata;

        // 判断是否跳过规范化结果，如果跳过，返回 400 BadRequestResult
        if (UnifyContext.CheckFailedNonUnify(context.HttpContext, actionDescriptor.MethodInfo,
                out IUnifyResultProvider unifyResult))
        {
            // 如果不启用 SuppressModelStateInvalidFilter，则跳过，理应手动验证
            if (!_apiBehaviorOptions.SuppressModelStateInvalidFilter)
            {
                finalContext.Result = _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
            }
            else
            {
                // 返回 JsonResult
                finalContext.Result = new JsonResult(validationMetadata.ValidationResult)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }
        else
        {
            if (!UnifyContext.CheckResponseNonUnify(context.HttpContext, actionDescriptor.MethodInfo,
                    out IUnifyResponseProvider unifyResponse))
            {
                await unifyResponse.ResponseValidationExceptionAsync(context, validationMetadata, context.HttpContext);
            }

            finalContext.Result = unifyResult.OnValidateFailed(context, validationMetadata);
        }

        return true;
    }
}
