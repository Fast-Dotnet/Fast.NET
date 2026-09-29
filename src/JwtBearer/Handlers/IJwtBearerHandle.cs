// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Fast.JwtBearer;

/// <summary>
/// 定义 JWT 身份验证和权限检查的自定义处理契约
/// </summary>
[SuppressSniffer]
public interface IJwtBearerHandle
{
    /// <summary>
    /// 执行身份验证后的附加授权检查
    /// </summary>
    /// <remarks>
    /// 调用此方法前，框架已完成令牌验证和自动刷新。返回 <see langword="false"/> 或抛出异常时
    /// 框架先调用 <see cref="AuthorizationHandlerContext.Fail()"/>，再调用
    /// <see cref="AuthorizeFailHandle"/> 生成可选的 MVC 响应。
    /// </remarks>
    /// <param name="context">当前授权处理上下文</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>授权通过时返回 <see langword="true"/>；返回 <see langword="false"/> 或抛出异常时进入失败处理</returns>
    Task<bool> AuthorizeHandle(AuthorizationHandlerContext context, HttpContext httpContext);

    /// <summary>
    /// 创建身份验证失败时的自定义响应数据
    /// </summary>
    /// <remarks>
    /// 授权始终保持失败。MVC 过滤器上下文可将非 null 结果写为 HTTP 401；
    /// 普通端点和 SignalR 由各自的授权管线处理拒绝响应，不强行写入 MVC Result。
    /// </remarks>
    /// <param name="context">当前授权处理上下文</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="exception">身份验证检查抛出的异常；没有捕获到异常时为 <see langword="null"/></param>
    /// <returns>自定义响应数据；使用默认失败处理时返回 <see langword="null"/></returns>
    Task<object> AuthorizeFailHandle(AuthorizationHandlerContext context, HttpContext httpContext, Exception exception);

    /// <summary>
    /// 判断当前请求是否满足指定授权要求
    /// </summary>
    /// <remarks>
    /// 返回 <see langword="false"/> 或抛出异常时，框架将调用 <see cref="PermissionFailHandle"/>
    /// </remarks>
    /// <param name="context">当前授权处理上下文</param>
    /// <param name="requirement">Fast 自身的权限要求；不用于代替角色、声明或第三方策略验证</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <returns>权限检查通过时返回 <see langword="true"/>；返回 <see langword="false"/> 或抛出异常时进入失败处理</returns>
    Task<bool> PermissionHandle(AuthorizationHandlerContext context, IAuthorizationRequirement requirement,
        HttpContext httpContext);

    /// <summary>
    /// 创建权限检查失败时的自定义响应数据
    /// </summary>
    /// <remarks>
    /// 权限始终保持失败。MVC 过滤器上下文可将非 null 结果写为 HTTP 403；
    /// 普通端点和 SignalR 由各自的授权管线处理拒绝响应，不强行写入 MVC Result。
    /// </remarks>
    /// <param name="context">当前授权处理上下文</param>
    /// <param name="requirement">验证失败的授权要求</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="exception">权限检查抛出的异常；没有捕获到异常时为 <see langword="null"/></param>
    /// <returns>自定义响应数据；使用默认失败处理时返回 <see langword="null"/></returns>
    Task<object> PermissionFailHandle(AuthorizationHandlerContext context, IAuthorizationRequirement requirement,
        HttpContext httpContext, Exception exception);
}
