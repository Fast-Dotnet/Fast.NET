// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;

namespace Fast.JwtBearer;

/// <summary>
/// JWT 配置
/// </summary>
[SuppressSniffer]
public sealed class JWTSettingsOptions : IPostConfigure
{
    /// <summary>
    /// 验证签发方密钥
    /// </summary>
    /// <remarks>默认 <see langword="true"/></remarks>
    public bool? ValidateIssuerSigningKey { get; set; }

    /// <summary>
    /// 签发方密钥
    /// </summary>
    public string IssuerSigningKey { get; set; }

    /// <summary>
    /// 验证签发方
    /// </summary>
    /// <remarks>默认 <see langword="true"/></remarks>
    public bool? ValidateIssuer { get; set; }

    /// <summary>
    /// 签发方
    /// </summary>
    public string ValidIssuer { get; set; }

    /// <summary>
    /// 验证签收方
    /// </summary>
    /// <remarks>默认 <see langword="true"/></remarks>
    public bool? ValidateAudience { get; set; }

    /// <summary>
    /// 签收方
    /// </summary>
    public string ValidAudience { get; set; }

    /// <summary>
    /// 验证生存期
    /// </summary>
    /// <remarks>默认 <see langword="true"/></remarks>
    public bool? ValidateLifetime { get; set; }

    /// <summary>
    /// 验证 AccessToken
    /// </summary>
    /// <remarks>
    /// <para>默认<see langword="false"/></para>
    /// <para>需调用 <see cref="JwtBearerUtil.SetExpiredToken"/> 才会验证</para>
    /// </remarks>
    public bool? ValidateAccessToken { get; set; }

    /// <summary>
    /// 过期时间容错值，解决服务器端时间不同步问题（秒）
    /// </summary>
    /// <remarks>默认 5 秒</remarks>
    public long? ClockSkew { get; set; }

    /// <summary>
    /// Token 过期时间（分钟）
    /// </summary>
    /// <remarks>默认 20 分钟</remarks>
    public long? TokenExpiredTime { get; set; }

    /// <summary>
    /// 刷新 Token 过期时间（分钟）
    /// </summary>
    /// <remarks>默认 1440 分钟(24 小时)</remarks>
    public long? RefreshTokenExpireTime { get; set; }

    /// <summary>
    /// 刷新 Token 时是否强制使用分布式缓存进行重放校验
    /// </summary>
    /// <remarks>
    /// <para>默认 <see langword="true"/>；标准注册流程未配置共享缓存时会自动使用进程内缓存</para>
    /// <para>如果应用绕过标准注册且没有提供 <c>IDistributedCache</c>，则拒绝刷新以避免 RefreshToken 被重复使用</para>
    /// </remarks>
    public bool? RequireRefreshTokenCache { get; set; }

    /// <summary>
    /// 加密算法
    /// </summary>
    /// <remarks>默认 HS256</remarks>
    public JwtBearerAlgorithmEnum? Algorithm { get; set; }

    /// <summary>
    /// 启用
    /// </summary>
    /// <remarks>默认<see langword="true"/></remarks>
    public bool? Enable { get; set; }

    /// <inheritdoc />
    public void PostConfigure()
    {
        ValidateIssuerSigningKey ??= true;
        ValidateIssuer ??= true;
        ValidIssuer ??= "Fast.NET.API";
        ValidateAudience ??= true;
        ValidAudience ??= "Fast.NET.Client";
        ValidateLifetime ??= true;
        ValidateAccessToken ??= false;
        ClockSkew ??= 5;
        TokenExpiredTime ??= 20;
        RefreshTokenExpireTime ??= 1440;
        RequireRefreshTokenCache ??= true;
        Algorithm ??= JwtBearerAlgorithmEnum.HS256;
        Enable ??= true;

        // 禁止继续使用框架内置的公共默认密钥，否则任何知道源码的人都能伪造令牌
        if (string.IsNullOrWhiteSpace(IssuerSigningKey))
        {
            throw new InvalidOperationException("JWTSettings:IssuerSigningKey 必须显式配置，且至少包含 32 个 UTF-8 字节。");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(IssuerSigningKey) < 32)
        {
            throw new InvalidOperationException("JWTSettings:IssuerSigningKey 至少需要 32 个 UTF-8 字节。");
        }
    }
}
