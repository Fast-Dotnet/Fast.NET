// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Fast.DynamicApplication")]


namespace Fast.UnifyResult;

/// <summary>
/// 规范化结果上下文
/// </summary>
[SuppressSniffer]
public static class UnifyContext
{
    private static readonly JsonSerializerOptions _validationSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true
    };

    /// <summary>
    /// 是否启用规范化结果
    /// </summary>
    public static bool EnabledUnifyHandler = false;

    /// <summary>
    /// 统一返回类型
    /// </summary>
    public static Type UnifyResultType => typeof(RestfulResult<>);

    /// <summary>
    /// 处理 Restful 响应状态码
    /// </summary>
    /// <param name="code">业务或枚举编码</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>处理 Restful 响应状态码</returns>
    public static RestfulResult<object> HandleRestfulStatusCode(int code, object data, string message, HttpContext httpContext)
    {
        return code switch
        {
            // 处理 400 状态码
            StatusCodes.Status400BadRequest => GetRestfulResult(StatusCodes.Status400BadRequest, false, data,
                message ?? "400 请求无效", httpContext),
            // 处理 401 状态码
            StatusCodes.Status401Unauthorized => GetRestfulResult(StatusCodes.Status401Unauthorized, false, data,
                message ?? "401 未经授权", httpContext),
            // 处理 403 状态码
            StatusCodes.Status403Forbidden => GetRestfulResult(StatusCodes.Status403Forbidden, false, data,
                message ?? "403 无操作权限", httpContext),
            // 处理 404 状态码
            StatusCodes.Status404NotFound => GetRestfulResult(StatusCodes.Status404NotFound, false, data, message ?? "404 无效的地址",
                httpContext),
            // 处理 405 状态码
            StatusCodes.Status405MethodNotAllowed => GetRestfulResult(StatusCodes.Status405MethodNotAllowed, false, data,
                message ?? "405 方法不被允许", httpContext),
            // 处理 409 状态码
            StatusCodes.Status409Conflict => GetRestfulResult(StatusCodes.Status409Conflict, false, data, message ?? "409 请求冲突",
                httpContext),
            // 处理 410 状态码
            StatusCodes.Status410Gone => GetRestfulResult(StatusCodes.Status410Gone, false, data, message ?? "410 资源已失效或永久删除",
                httpContext),
            // 处理 415 状态码
            StatusCodes.Status415UnsupportedMediaType => GetRestfulResult(StatusCodes.Status415UnsupportedMediaType, false, data,
                message ?? "415 不支持的媒体类型", httpContext),
            // 处理 422 状态码
            StatusCodes.Status422UnprocessableEntity => GetRestfulResult(StatusCodes.Status422UnprocessableEntity, false, data,
                message ?? "422 请求语义错误，参数验证失败", httpContext),
            // 处理 429 状态码
            StatusCodes.Status429TooManyRequests => GetRestfulResult(StatusCodes.Status429TooManyRequests, false, data,
                message ?? "429 频繁请求", httpContext),
            // 处理 500 状态码
            StatusCodes.Status500InternalServerError => GetRestfulResult(StatusCodes.Status500InternalServerError, false, data,
                message ?? "500 服务器内部错误", httpContext),
            // 处理 502 状态码
            StatusCodes.Status502BadGateway => GetRestfulResult(StatusCodes.Status502BadGateway, false, data,
                message ?? "502 网关错误", httpContext),
            // 处理 503 状态码
            StatusCodes.Status503ServiceUnavailable => GetRestfulResult(StatusCodes.Status503ServiceUnavailable, false, data,
                message ?? "503 服务不可用", httpContext),
            // 处理 504 状态码
            StatusCodes.Status504GatewayTimeout => GetRestfulResult(StatusCodes.Status504GatewayTimeout, false, data,
                message ?? "504 网关超时", httpContext),
            _ => GetRestfulResult(StatusCodes.Status500InternalServerError, false, data, message, httpContext)
        };
    }

    /// <summary>
    /// 获取规范化 RESTful 风格返回值
    /// </summary>
    /// <param name="code">业务或枚举编码</param>
    /// <param name="success">操作是否成功</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="message">要记录或返回的消息</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>获取到的规范化 RESTful 风格返回值</returns>
    public static RestfulResult<object> GetRestfulResult(int code, bool success, object data, object message,
        HttpContext httpContext)
    {
        // 从请求响应头部中获取时间戳
        long timestamp = httpContext.UnifyResponseTimestamp();

        return new RestfulResult<object>
        {
            Code = code,
            Success = success,
            Data = data,
            Message = message,
            Timestamp = timestamp
        };
    }

    /// <summary>
    /// 检查请求成功是否进行规范化处理
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="method">当前动作方法</param>
    /// <param name="unifyResult">可用的规范化结果提供器；跳过处理时为 <see langword="null"/></param>
    /// <param name="isWebRequest">是否需要从请求服务中解析结果提供器</param>
    /// <returns>应跳过规范化处理时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    internal static bool CheckSucceededNonUnify(HttpContext httpContext, MethodInfo method, out IUnifyResultProvider unifyResult,
        bool isWebRequest = true)
    {
        // 判断返回类型是否包含了规范化处理的返回类型
        bool isSkip = method.GetRealReturnType().HasImplementedRawGeneric(UnifyResultType);

        Type nonUnifyAttributeType = typeof(NonUnifyAttribute);

        // 使用 IsAssignableFrom 同时识别 NonUnifyAttribute 及其自定义派生特性
        Type producesResponseTypeAttributeType = typeof(ProducesResponseTypeAttribute);
        Type iApiResponseMetadataProviderType = typeof(IApiResponseMetadataProvider);
        if (!isSkip
            && method.CustomAttributes.Any(a =>
                nonUnifyAttributeType.IsAssignableFrom(a.AttributeType)
                || producesResponseTypeAttributeType.IsAssignableFrom(a.AttributeType)
                || iApiResponseMetadataProviderType.IsAssignableFrom(a.AttributeType)))
        {
            isSkip = true;
        }

        // 判断方法所在的类是否贴有 NonUnifyAttribute 特性
        if (!isSkip && method.ReflectedType?.IsDefined(nonUnifyAttributeType, true) == true)
        {
            isSkip = true;
        }

        // OData 自行协商响应格式，不能套用普通 MVC 的统一结果结构
        if (!isSkip && method.ReflectedType?.Assembly.GetName().Name?.StartsWith("Microsoft.AspNetCore.OData") == true)
        {
            isSkip = true;
        }

        // 判断是否为 Web 请求
        if (!isWebRequest)
        {
            unifyResult = null;
            return isSkip;
        }

        if (isSkip)
        {
            unifyResult = null;
        }
        else
        {
            unifyResult = httpContext?.RequestServices.GetService<IUnifyResultProvider>();
        }

        return unifyResult == null || isSkip;
    }

    /// <summary>
    /// 检查请求失败（验证失败、抛异常）是否进行规范化处理
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="method">发生验证失败或异常的动作方法</param>
    /// <param name="unifyResult">可用的规范化结果提供器；跳过处理时为 <see langword="null"/></param>
    /// <returns>应跳过规范化处理时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    internal static bool CheckFailedNonUnify(HttpContext httpContext, MethodInfo method, out IUnifyResultProvider unifyResult)
    {
        // 使用 IsAssignableFrom 同时识别框架特性及其自定义派生特性
        Type nonUnifyAttributeType = typeof(NonUnifyAttribute);

        Type producesResponseTypeAttributeType = typeof(ProducesResponseTypeAttribute);
        Type iApiResponseMetadataProviderType = typeof(IApiResponseMetadataProvider);

        bool isSkip = !method.CustomAttributes.Any(a =>
                          nonUnifyAttributeType.IsAssignableFrom(a.AttributeType)
                          || producesResponseTypeAttributeType.IsAssignableFrom(a.AttributeType)
                          || iApiResponseMetadataProviderType.IsAssignableFrom(a.AttributeType))
                      && method.ReflectedType?.IsDefined(nonUnifyAttributeType, true) == true;

        // OData 自行协商响应格式，不能套用普通 MVC 的统一结果结构
        if (!isSkip && method.ReflectedType?.Assembly.GetName().Name?.StartsWith("Microsoft.AspNetCore.OData") == true)
        {
            isSkip = true;
        }

        if (isSkip)
        {
            unifyResult = null;
        }
        else
        {
            unifyResult = httpContext.RequestServices.GetService<IUnifyResultProvider>();
        }

        return unifyResult == null || isSkip;
    }

    /// <summary>
    /// 检查请求响应数据是否进行规范化处理
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="method">当前动作方法</param>
    /// <param name="unifyResponse">可用的规范化响应提供器；跳过处理时为 <see langword="null"/></param>
    /// <returns>应跳过规范化处理时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    internal static bool CheckResponseNonUnify(HttpContext httpContext, MethodInfo method,
        out IUnifyResponseProvider unifyResponse)
    {
        // 使用 IsAssignableFrom 同时识别框架特性及其自定义派生特性
        Type nonUnifyAttributeType = typeof(NonUnifyAttribute);

        Type producesResponseTypeAttributeType = typeof(ProducesResponseTypeAttribute);
        Type iApiResponseMetadataProviderType = typeof(IApiResponseMetadataProvider);

        bool isSkip = !method.CustomAttributes.Any(a =>
                          nonUnifyAttributeType.IsAssignableFrom(a.AttributeType)
                          || producesResponseTypeAttributeType.IsAssignableFrom(a.AttributeType)
                          || iApiResponseMetadataProviderType.IsAssignableFrom(a.AttributeType))
                      && method.ReflectedType?.IsDefined(nonUnifyAttributeType, true) == true;

        // OData 自行协商响应格式，不能套用普通 MVC 的统一结果结构
        if (!isSkip && method.ReflectedType?.Assembly.GetName().Name?.StartsWith("Microsoft.AspNetCore.OData") == true)
        {
            isSkip = true;
        }

        if (isSkip)
        {
            unifyResponse = null;
        }
        else
        {
            unifyResponse = httpContext.RequestServices.GetService<IUnifyResponseProvider>();
        }

        return unifyResponse == null || isSkip;
    }

    /// <summary>
    /// 检查短路状态码（>=400）是否进行规范化处理
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="unifyResult">可用的规范化结果提供器；跳过处理时为 <see langword="null"/></param>
    /// <returns>应跳过规范化处理时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    internal static bool CheckStatusCodeNonUnify(HttpContext httpContext, out IUnifyResultProvider unifyResult)
    {
        // 获取终点路由特性
        IEndpointFeature endpointFeature = httpContext.Features.Get<IEndpointFeature>();
        if (endpointFeature == null)
        {
            unifyResult = null;
            return true;
        }

        Type nonUnifyAttributeType = typeof(NonUnifyAttribute);

        // 判断终点路由是否存在 NonUnifyAttribute 特性
        bool isSkip = httpContext.GetMetadata(nonUnifyAttributeType) != null;

        // 判断终点路由是否存在 NonUnifyAttribute 特性
        if (!isSkip && endpointFeature.Endpoint?.Metadata.GetMetadata(nonUnifyAttributeType) != null)
        {
            isSkip = true;
        }

        // 判断请求头部是否包含 odata.metadata=
        if (!isSkip
            && httpContext.Request.Headers["accept"].ToString().Contains("odata.metadata=", StringComparison.OrdinalIgnoreCase))
        {
            isSkip = true;
        }

        // 判断请求头部是否包含 odata.streaming=
        if (!isSkip
            && httpContext.Request.Headers["accept"].ToString().Contains("odata.streaming=", StringComparison.OrdinalIgnoreCase))
        {
            isSkip = true;
        }

        if (isSkip)
        {
            unifyResult = null;
        }
        else
        {
            unifyResult = httpContext.RequestServices.GetService<IUnifyResultProvider>();
        }

        return unifyResult == null || isSkip;
    }

    /// <summary>
    /// 检查是否是有效的结果（可进行规范化的结果）
    /// </summary>
    /// <param name="result">MVC 动作结果</param>
    /// <param name="data">可参与规范化的数据；结果类型不受支持时为 <see langword="null"/></param>
    /// <returns>结果可转换为统一响应时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    internal static bool CheckValidResult(IActionResult result, out object data)
    {
        data = null;

        // 排除以下结果，跳过规范化处理
        bool isDataResult = result switch
        {
            ViewResult => false,
            PartialViewResult => false,
            FileResult => false,
            ChallengeResult => false,
            SignInResult => false,
            SignOutResult => false,
            RedirectToPageResult => false,
            RedirectToRouteResult => false,
            RedirectResult => false,
            RedirectToActionResult => false,
            LocalRedirectResult => false,
            ForbidResult => false,
            ViewComponentResult => false,
            PageResult => false,
            NotFoundResult => false,
            NotFoundObjectResult => false,
            _ => true
        };

        // 仅从携带正文的结果类型中提取数据，其他可规范化结果由调用方按空数据处理
        if (isDataResult)
        {
            data = result switch
            {
                // 处理内容结果
                ContentResult content => content.Content,
                // 处理对象结果
                ObjectResult obj => obj.Value,
                // 处理 JSON 对象
                JsonResult json => json.Value,
                _ => null
            };
        }

        return isDataResult;
    }

    /// <summary>
    /// 获取验证错误信息
    /// </summary>
    /// <param name="errors">模型状态、验证问题详情、错误字典或普通错误对象</param>
    /// <returns>包含完整错误集合及首个错误位置的验证元数据</returns>
    internal static ValidationMetadata GetValidationMetadata(object errors)
    {
        ModelStateDictionary _modelState = null;
        object validationResults = null;
        string message, firstErrorMessage, firstErrorProperty = null;

        // 判断是否是集合类型
        if (errors is IEnumerable and not string)
        {
            // 如果是模型验证字典类型
            if (errors is ModelStateDictionary modelState)
            {
                _modelState = modelState;
                // 将验证错误整理为字典并序列化为 JSON
                validationResults = modelState.Where(u => modelState[u.Key]!.ValidationState == ModelValidationState.Invalid)
                    .ToDictionary(u => u.Key, u => modelState[u.Key]?.Errors.Select(c => c.ErrorMessage).ToArray());
            }
            // 如果是 ValidationProblemDetails 特殊类型
            else if (errors is ValidationProblemDetails validation)
            {
                validationResults = validation.Errors.ToDictionary(u => u.Key, u => u.Value.ToArray());
            }
            // 如果是字典类型
            else if (errors is Dictionary<string, string[]> dicResults)
            {
                validationResults = dicResults;
            }

            if (validationResults is Dictionary<string, string[]> resultDictionary)
            {
                message = JsonSerializer.Serialize(resultDictionary, _validationSerializerOptions);

                // 模型状态可能没有错误项；避免用 First() 将原始验证失败覆盖成新的异常
                KeyValuePair<string, string[]> firstError = resultDictionary.FirstOrDefault(pair => pair.Value?.Length > 0);
                firstErrorProperty = firstError.Key;
                firstErrorMessage = firstError.Value?.FirstOrDefault();
            }
            else
            {
                validationResults = firstErrorMessage = message = errors?.ToString();
            }
        }
        // 其他类型
        else
        {
            validationResults = firstErrorMessage = message = errors?.ToString();
        }

        return new ValidationMetadata
        {
            ValidationResult = validationResults,
            Message = message,
            ModelState = _modelState,
            FirstErrorProperty = firstErrorProperty,
            FirstErrorMessage = firstErrorMessage
        };
    }

    /// <summary>
    /// 获取异常元数据
    /// </summary>
    /// <param name="context">包含当前异常和请求信息的 MVC 上下文</param>
    /// <returns>从异常类型及上下文解析出的统一异常元数据</returns>
    internal static ExceptionMetadata GetExceptionMetadata(ActionContext context)
    {
        object errorCode = null;
        object originErrorCode = null;
        object errors = null;
        object data = null;

        int statusCode = StatusCodes.Status500InternalServerError;
        // 判断是否是验证异常
        bool isValidationException = false;

        Exception exception = null;

        // 判断是否是 ExceptionContext
        if (context is ExceptionContext exceptionContext)
        {
            exception = exceptionContext.Exception;
        }

        // 判断是否是 ActionExecutedContext
        if (context is ActionExecutedContext actionExecutedContext)
        {
            exception = actionExecutedContext.Exception;
        }

        // 判断是否是 用户友好异常
        if (exception is UserFriendlyException friendlyException)
        {
            errorCode = friendlyException.ErrorCode;
            originErrorCode = friendlyException.OriginErrorCode;
            statusCode = friendlyException.StatusCode;
            isValidationException = friendlyException.ValidationException;
            errors = friendlyException.ErrorMessage;
            data = friendlyException.Data;
        }

        // 处理非验证失败的错误对象
        if (!isValidationException)
        {
            errors = exception?.InnerException?.Message ?? exception?.Message ?? "Internal Server Error";
        }

        return new ExceptionMetadata
        {
            StatusCode = statusCode,
            ErrorCode = errorCode,
            OriginErrorCode = originErrorCode,
            Errors = errors,
            Data = data
        };
    }
}
