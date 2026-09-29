// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;


namespace Fast.JwtBearer;

/// <summary>
/// JwtBearer 工具类
/// </summary>
public static class JwtBearerUtil
{
    private const string AccessTokenBlacklistCacheKeyPrefix = "BLACKLIST_ACCESS_TOKEN:";
    private const string RefreshTokenBlacklistCacheKeyPrefix = "BLACKLIST_REFRESH_TOKEN:";
    private const int RefreshTokenLockCount = 64;

    /// <summary>
    /// JWT 载荷序列化配置
    /// </summary>
    private static readonly JsonSerializerOptions _payloadSerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 避免同一进程内的并发请求同时消费同一个 RefreshToken
    /// </summary>
    private static readonly SemaphoreSlim[] _refreshTokenLocks = Enumerable
        .Range(0, RefreshTokenLockCount)
        .Select(_ => new SemaphoreSlim(1, 1))
        .ToArray();

    /// <summary>
    /// 日期类型的 Claim 类型
    /// </summary>
    public static readonly string[] DateTypeClaimTypes =
    [
        JwtRegisteredClaimNames.Iat, JwtRegisteredClaimNames.Nbf, JwtRegisteredClaimNames.Exp
    ];

    /// <summary>
    /// 刷新 Token 身份标识
    /// </summary>
    public static readonly string[] RefreshTokenClaims = ["f", "e", "s", "l", "k"];

    /// <summary>
    /// 生成 Token 验证参数
    /// </summary>
    /// <param name="jwtSettings">JWT 签发与验证配置</param>
    /// <returns>生成的 Token 验证参数</returns>
    public static TokenValidationParameters CreateTokenValidationParameters(JWTSettingsOptions jwtSettings)
    {
        ArgumentNullException.ThrowIfNull(jwtSettings);
        if (string.IsNullOrWhiteSpace(jwtSettings.IssuerSigningKey))
        {
            throw new InvalidOperationException("JWT 签名密钥不能为空。");
        }

        string algorithm = jwtSettings.Algorithm?.ToString() ?? SecurityAlgorithms.HmacSha256;
        return new TokenValidationParameters
        {
            // 验证签发方密钥
            ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey ?? true,
            // 签发方密钥
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey)),
            // 验证签发方
            ValidateIssuer = jwtSettings.ValidateIssuer ?? true,
            // 设置签发方
            ValidIssuer = jwtSettings.ValidIssuer,
            // 验证签收方
            ValidateAudience = jwtSettings.ValidateAudience ?? true,
            // 设置接收方
            ValidAudience = jwtSettings.ValidAudience,
            // 验证生存期
            ValidateLifetime = jwtSettings.ValidateLifetime ?? true,
            // 过期时间容错值
            ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkew ?? 5),
            // 只接受配置的算法，避免同一密钥被其他 JWT 算法复用
            ValidAlgorithms = new[] {algorithm},
            RequireSignedTokens = true,
            RequireExpirationTime = true
        };
    }

    /// <summary>
    /// 生成 Token
    /// </summary>
    /// <param name="payload">要写入令牌的载荷</param>
    /// <param name="expiredTime">令牌过期时间</param>
    /// <returns>生成的 Token</returns>
    public static string GenerateToken(IDictionary<string, object> payload, long? expiredTime = null)
    {
        ArgumentNullException.ThrowIfNull(payload);
        JWTSettingsOptions jwtSettings =
            Penetrates.JWTSettings ?? throw new InvalidOperationException("JWT 尚未配置，请先注册 JWTSettings。");
        if (string.IsNullOrWhiteSpace(jwtSettings.IssuerSigningKey))
        {
            throw new InvalidOperationException("JWT 签名密钥不能为空，禁止生成未签名 Token。");
        }

        DateTimeOffset datetimeOffset = DateTimeOffset.UtcNow;

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Iat))
        {
            payload.Add(JwtRegisteredClaimNames.Iat, datetimeOffset.ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Nbf))
        {
            payload.Add(JwtRegisteredClaimNames.Nbf, datetimeOffset.ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Exp))
        {
            long minute = expiredTime ?? jwtSettings.TokenExpiredTime ?? 20;
            payload.Add(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(minute).ToUnixTimeSeconds());
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Iss))
        {
            payload.Add(JwtRegisteredClaimNames.Iss, jwtSettings.ValidIssuer);
        }

        if (!payload.ContainsKey(JwtRegisteredClaimNames.Aud))
        {
            payload.Add(JwtRegisteredClaimNames.Aud, jwtSettings.ValidAudience);
        }

        // 处理 JwtPayload 序列化不一致问题
        string stringPayload = payload is JwtPayload jwtPayload
            ? jwtPayload.SerializeToJson()
            : JsonSerializer.Serialize(payload, _payloadSerializerOptions);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey));
        var credentials = new SigningCredentials(securityKey, jwtSettings.Algorithm?.ToString() ?? SecurityAlgorithms.HmacSha256);

        var tokenHandler = new JsonWebTokenHandler();
        return tokenHandler.CreateToken(stringPayload, credentials);
    }

    /// <summary>
    /// 生成刷新 Token
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>生成的刷新 Token</returns>
    public static string GenerateRefreshToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access Token 不能为空。", nameof(accessToken));
        }

        // 分割 Token
        string[] tokenParagraphs = accessToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokenParagraphs.Length != 3)
        {
            throw new ArgumentException("Access Token 不是有效的 JWT 格式。", nameof(accessToken));
        }

        int payloadLength = tokenParagraphs[1].Length;
        if (payloadLength == 0)
        {
            throw new ArgumentException("Access Token 的载荷不能为空。", nameof(accessToken));
        }

        int maxLength = Math.Min(12, payloadLength);
        int minLength = Math.Min(3, maxLength);
        int l = minLength == maxLength ? minLength : RandomNumberGenerator.GetInt32(minLength, maxLength + 1);
        int maxStart = payloadLength - l;
        int s = maxStart == 0 ? 0 : RandomNumberGenerator.GetInt32(maxStart + 1);

        var payload = new Dictionary<string, object>
        {
            {"f", tokenParagraphs[0]},
            {"e", tokenParagraphs[2]},
            {"s", s},
            {"l", l},
            {"k", tokenParagraphs[1].Substring(s, l)}
        };

        return GenerateToken(payload, Penetrates.JWTSettings?.RefreshTokenExpireTime ?? 43200);
    }

    /// <summary>
    /// 获取 JWT Bearer Token
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="headerKey">承载令牌的请求头名称</param>
    /// <param name="tokenPrefix">请求头中位于令牌之前的前缀</param>
    /// <returns>获取到的 JWT Bearer Token</returns>
    public static string GetJwtBearerToken(HttpContext httpContext, string headerKey = "Authorization",
        string tokenPrefix = "Bearer ")
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        if (string.IsNullOrWhiteSpace(headerKey))
        {
            throw new ArgumentException("Token 请求头名称不能为空。", nameof(headerKey));
        }

        if (string.IsNullOrEmpty(tokenPrefix))
        {
            throw new ArgumentException("Token 前缀不能为空。", nameof(tokenPrefix));
        }

        // 判断请求报文头中是否有 "Authorization" 报文头
        string bearerToken = httpContext.Request.Headers[headerKey].ToString();
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return null;
        }

        int prefixLength = tokenPrefix.Length;
        return bearerToken.StartsWith(tokenPrefix, StringComparison.OrdinalIgnoreCase) && bearerToken.Length > prefixLength
            ? bearerToken[prefixLength..]
            : null;
    }

    /// <summary>
    /// 生成不包含 Token 明文的缓存键，避免缓存监控和诊断信息泄露凭据
    /// </summary>
    /// <param name="cacheKeyPrefix">令牌缓存键使用的前缀</param>
    /// <param name="token">要解析或验证的令牌</param>
    /// <returns>生成的不包含 Token 明文的缓存键，避免缓存监控和诊断信息泄露凭据</returns>
    private static string CreateTokenCacheKey(string cacheKeyPrefix, string token)
    {
        byte[] tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return cacheKeyPrefix + Convert.ToHexString(tokenHash);
    }

    /// <summary>
    /// 使用哈希缓存键读取 Token 状态
    /// </summary>
    /// <param name="distributedCache">用于保存令牌状态的分布式缓存</param>
    /// <param name="cacheKeyPrefix">令牌缓存键使用的前缀</param>
    /// <param name="token">要解析或验证的令牌</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>表示异步使用哈希缓存键读取 Token 状态的任务，任务结果为使用哈希缓存键读取 Token 状态</returns>
    private static async Task<string> GetTokenCacheValueAsync(IDistributedCache distributedCache, string cacheKeyPrefix,
        string token, CancellationToken cancellationToken = default)
    {
        return await distributedCache.GetStringAsync(CreateTokenCacheKey(cacheKeyPrefix, token), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 消费 RefreshToken，防止同一个令牌被重复换取新令牌
    /// </summary>
    /// <param name="distributedCache">用于保存令牌状态的分布式缓存</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="absoluteExpiration">缓存项的绝对过期时间</param>
    /// <param name="reuseLeewaySeconds">刷新令牌并发复用的宽限时长，单位为秒</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>成功取得目标值时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    private static async Task<bool> TryConsumeRefreshTokenAsync(IDistributedCache distributedCache, string refreshToken,
        DateTimeOffset absoluteExpiration, long reuseLeewaySeconds, CancellationToken cancellationToken = default)
    {
        DateTimeOffset nowTime = DateTimeOffset.UtcNow;
        if (absoluteExpiration <= nowTime)
        {
            return false;
        }

        string cacheKey = CreateTokenCacheKey(RefreshTokenBlacklistCacheKeyPrefix, refreshToken);
        int lockIndex = (int)((uint)StringComparer.Ordinal.GetHashCode(cacheKey) % _refreshTokenLocks.Length);
        SemaphoreSlim refreshTokenLock = _refreshTokenLocks[lockIndex];

        await refreshTokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string cachedValue = await GetTokenCacheValueAsync(distributedCache,
                    RefreshTokenBlacklistCacheKeyPrefix, refreshToken, cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(cachedValue))
            {
                // 默认不允许重放；仅保留显式传入容差值时的兼容行为
                if (reuseLeewaySeconds <= 0 || !long.TryParse(cachedValue, out long refreshTicks))
                {
                    return false;
                }

                var refreshTime = new DateTimeOffset(refreshTicks, TimeSpan.Zero);
                TimeSpan elapsed = nowTime - refreshTime;
                return elapsed >= TimeSpan.Zero && elapsed.TotalSeconds <= reuseLeewaySeconds;
            }

            await distributedCache.SetStringAsync(cacheKey, nowTime.Ticks.ToString(),
                    new DistributedCacheEntryOptions {AbsoluteExpiration = absoluteExpiration}, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
        finally
        {
            refreshTokenLock.Release();
        }
    }

    /// <summary>
    /// 验证 Token
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>验证 Token</returns>
    public static (bool IsValid, JsonWebToken Token, TokenValidationResult validationResult) Validate(string accessToken)
    {
        return ValidateAsync(accessToken).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步验证 Token
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>表示异步验证 Token 的任务，任务结果为验证 Token</returns>
    public static async Task<(bool IsValid, JsonWebToken Token, TokenValidationResult validationResult)> ValidateAsync(
        string accessToken)
    {
        if (Penetrates.JWTSettings == null || string.IsNullOrWhiteSpace(accessToken))
        {
            return (false, null, null);
        }

        try
        {
            TokenValidationParameters validationParameters = CreateTokenValidationParameters(Penetrates.JWTSettings);
            return await ValidateAsync(accessToken, validationParameters).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 保持工具方法原有契约：格式、配置或签名错误均通过 IsValid=false 返回
            return (false, null, null);
        }
    }

    /// <summary>
    /// 使用指定参数验证 Token
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="validationParameters">令牌签名和声明验证参数</param>
    /// <returns>验证是否成功、读取到的令牌及完整验证结果。</returns>
    private static async Task<(bool IsValid, JsonWebToken Token, TokenValidationResult validationResult)> ValidateAsync(
        string accessToken, TokenValidationParameters validationParameters)
    {
        var tokenHandler = new JsonWebTokenHandler();
        TokenValidationResult tokenValidationResult = await ValidateTokenAsync(tokenHandler, accessToken, validationParameters)
            .ConfigureAwait(false);
        if (!tokenValidationResult.IsValid)
        {
            return (false, null, tokenValidationResult);
        }

        var jsonWebToken = tokenValidationResult.SecurityToken as JsonWebToken;
        return (jsonWebToken != null, jsonWebToken, tokenValidationResult);
    }

    /// <summary>
    /// 兼容不同 IdentityModel 版本的 Token 验证 API
    /// </summary>
    /// <param name="tokenHandler">用于读取并验证令牌的处理器</param>
    /// <param name="accessToken">访问令牌</param>
    /// <param name="validationParameters">令牌签名和声明验证参数</param>
    /// <returns>包含有效性、声明身份及验证异常的验证结果。</returns>
    private static Task<TokenValidationResult> ValidateTokenAsync(JsonWebTokenHandler tokenHandler, string accessToken,
        TokenValidationParameters validationParameters)
    {
#if NET8_0_OR_GREATER
        return tokenHandler.ValidateTokenAsync(accessToken, validationParameters);
#else
        return Task.FromResult(tokenHandler.ValidateToken(accessToken, validationParameters));
#endif
    }

    /// <summary>
    /// 验证 Token
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="token">要解析或验证的令牌</param>
    /// <param name="headerKey">承载令牌的请求头名称</param>
    /// <param name="tokenPrefix">请求头中位于令牌之前的前缀</param>
    /// <returns>请求中存在且验证通过的访问令牌返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool ValidateJwtBearerToken(DefaultHttpContext httpContext, out JsonWebToken token,
        string headerKey = "Authorization", string tokenPrefix = "Bearer ")
    {
        // 获取 token
        string accessToken = GetJwtBearerToken(httpContext, headerKey, tokenPrefix);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            token = null;
            return false;
        }

        // 验证 token
        (bool IsValid, JsonWebToken Token, _) = Validate(accessToken);
        token = IsValid ? Token : null;

        return IsValid;
    }

    /// <summary>
    /// 读取 Token，不含验证
    /// </summary>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>读取到的 Token，不含验证</returns>
    public static JsonWebToken ReadJwtToken(string accessToken)
    {
        var tokenHandler = new JsonWebTokenHandler();
        if (tokenHandler.CanReadToken(accessToken))
        {
            return tokenHandler.ReadJsonWebToken(accessToken);
        }

        return null;
    }

    /// <summary>
    /// 读取 Token
    /// </summary>
    /// <remarks>仅解析令牌，不会验证签名、签发方或有效期；安全决策请使用 <see cref="Validate"/></remarks>
    /// <param name="accessToken">访问令牌</param>
    /// <returns>读取到的 Token</returns>
    public static JwtSecurityToken SecurityReadJwtToken(string accessToken)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(accessToken);
        return jwtSecurityToken;
    }

    /// <summary>
    /// 通过过期 Token 和 刷新 Token 换取新的 Token
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="expiredToken">已过期但签名仍需验证的访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="expiredTime">令牌过期时间</param>
    /// <param name="clockSkew">允许同一刷新令牌重复提交的兼容容差（秒）；默认 0，禁止重放。</param>
    /// <returns>通过过期 Token 和 刷新 Token 换取新的 Token</returns>
    public static string Exchange(HttpContext httpContext, string expiredToken, string refreshToken, long? expiredTime = null,
        long? clockSkew = null)
    {
        return ExchangeAsync(httpContext, expiredToken, refreshToken, expiredTime, clockSkew)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步使用过期 Token 和刷新 Token 换取新的 Token
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="expiredToken">已过期但签名仍需验证的访问令牌</param>
    /// <param name="refreshToken">刷新令牌</param>
    /// <param name="expiredTime">令牌过期时间</param>
    /// <param name="clockSkew">允许同一刷新令牌重复提交的兼容容差（秒）；默认 0，禁止重放。</param>
    /// <returns>表示异步使用过期 Token 和刷新 Token 换取新的 Token 的任务，任务结果为使用过期 Token 和刷新 Token 换取新的 Token</returns>
    public static async Task<string> ExchangeAsync(HttpContext httpContext, string expiredToken, string refreshToken,
        long? expiredTime = null, long? clockSkew = null)
    {
        if (Penetrates.JWTSettings == null
            || httpContext == null
            || string.IsNullOrWhiteSpace(expiredToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        // 原访问令牌必须签名、签发方和接收方均有效，仅在此处临时忽略生命周期
        // 不能直接把普通 Validate=false 当作“已过期”，否则被篡改或伪造的令牌也会进入刷新流程
        TokenValidationParameters expiredValidationParameters = CreateTokenValidationParameters(Penetrates.JWTSettings);
        expiredValidationParameters.ValidateLifetime = false;
        (bool isExpiredTokenAuthentic, JsonWebToken expiredTokenObj, _) =
            await ValidateAsync(expiredToken, expiredValidationParameters).ConfigureAwait(false);
        if (!isExpiredTokenAuthentic
            || !expiredTokenObj.TryGetPayloadValue(JwtRegisteredClaimNames.Exp, out long expiredAt)
            || DateTimeOffset.FromUnixTimeSeconds(expiredAt) > DateTimeOffset.UtcNow)
        {
            return null;
        }

        (bool isRefreshTokenValid, JsonWebToken refreshTokenObj, _) = await ValidateAsync(refreshToken).ConfigureAwait(false);
        if (!isRefreshTokenValid)
        {
            return null;
        }

        if (!refreshTokenObj.TryGetPayloadValue("s", out int start)
            || !refreshTokenObj.TryGetPayloadValue("l", out int length)
            || !refreshTokenObj.TryGetPayloadValue("f", out string refreshHeader)
            || !refreshTokenObj.TryGetPayloadValue("e", out string refreshSignature)
            || !refreshTokenObj.TryGetPayloadValue("k", out string refreshPayloadFragment)
            || !refreshTokenObj.TryGetPayloadValue(JwtRegisteredClaimNames.Exp, out long refreshExpiresAt))
        {
            return null;
        }

        string[] tokenParagraphs = expiredToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (tokenParagraphs.Length != 3)
        {
            return null;
        }

        if (start < 0 || length < 0 || start > tokenParagraphs[1].Length - length)
        {
            return null;
        }

        if (!string.Equals(refreshHeader, tokenParagraphs[0], StringComparison.Ordinal)
            || !string.Equals(refreshSignature, tokenParagraphs[2], StringComparison.Ordinal)
            || !string.Equals(tokenParagraphs[1].Substring(start, length), refreshPayloadFragment, StringComparison.Ordinal))
        {
            return null;
        }

        IDistributedCache distributedCache = httpContext.RequestServices.GetService<IDistributedCache>();
        if (distributedCache == null)
        {
            // 默认安全失败：没有共享状态就无法识别 RefreshToken 重放
            if (Penetrates.JWTSettings.RequireRefreshTokenCache == true)
            {
                return null;
            }
        }
        else if (!await TryConsumeRefreshTokenAsync(distributedCache, refreshToken,
                         DateTimeOffset.FromUnixTimeSeconds(refreshExpiresAt), Math.Max(0, clockSkew ?? 0),
                         httpContext.RequestAborted)
                     .ConfigureAwait(false))
        {
            return null;
        }

        // 上面已完成签名验证，此时才可以读取原令牌载荷并签发新令牌
        JwtPayload payload = SecurityReadJwtToken(expiredToken).Payload;
        foreach (string innerKey in DateTypeClaimTypes)
        {
            payload.Remove(innerKey);
        }

        return GenerateToken(payload, expiredTime);
    }

    /// <summary>
    /// 标记过期 Token
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="expiredToken">已过期但签名仍需验证的访问令牌</param>
    public static void SetExpiredToken(HttpContext httpContext, string expiredToken)
    {
        SetExpiredTokenAsync(httpContext, expiredToken).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步标记失效 Token
    /// </summary>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="expiredToken">已过期但签名仍需验证的访问令牌</param>
    /// <returns>黑名单写入完成的任务；无有效令牌或无缓存时不写入。</returns>
    public static async Task SetExpiredTokenAsync(HttpContext httpContext, string expiredToken)
    {
        if (string.IsNullOrEmpty(expiredToken))
        {
            return;
        }

        // 标记过期 必须原 Token 是有效的
        (bool _isValid, JsonWebToken accessTokenObj, _) = await ValidateAsync(expiredToken).ConfigureAwait(false);
        if (!_isValid)
        {
            return;
        }

        if (!accessTokenObj.TryGetPayloadValue(JwtRegisteredClaimNames.Exp, out long expiresAt))
        {
            return;
        }

        // 黑名单有效期必须覆盖 Token 验证的 ClockSkew 容错窗口
        DateTimeOffset blacklistExpiration = DateTimeOffset.FromUnixTimeSeconds(expiresAt)
                                             + TimeSpan.FromSeconds(Penetrates.JWTSettings.ClockSkew ?? 5);
        DateTimeOffset nowTime = DateTimeOffset.UtcNow;
        TimeSpan blacklistLifetime = blacklistExpiration - nowTime;
        if (blacklistLifetime <= TimeSpan.Zero)
        {
            return;
        }

        IDistributedCache distributedCache = httpContext?.RequestServices.GetService<IDistributedCache>();

        // 标记失效
        if (distributedCache != null)
        {
            await distributedCache.SetStringAsync(CreateTokenCacheKey(AccessTokenBlacklistCacheKeyPrefix, expiredToken),
                    nowTime.Ticks.ToString(), new DistributedCacheEntryOptions
                    {
                        // 使用相对过期时间，避免检查后写入前跨过绝对过期点
                        AbsoluteExpirationRelativeToNow = blacklistLifetime
                    }, httpContext.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 自动刷新 Token 信息
    /// </summary>
    /// <param name="context">当前授权处理上下文</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="expiredTime">令牌过期时间</param>
    /// <param name="tokenPrefix">请求头中位于令牌之前的前缀</param>
    /// <param name="clockSkew">允许同一刷新 Token 重复提交的兼容容差（秒），默认 0（禁止重放）</param>
    /// <returns>当前身份有效、允许匿名或刷新成功时返回 <see langword="true"/>，表示可以继续后续授权；身份校验或刷新失败时返回 <see langword="false"/></returns>
    public static bool AutoRefreshToken(AuthorizationHandlerContext context, HttpContext httpContext, long? expiredTime = null,
        string tokenPrefix = "Bearer ", long? clockSkew = null)
    {
        return AutoRefreshTokenAsync(context, httpContext, expiredTime, tokenPrefix, clockSkew)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步自动刷新 Token 信息
    /// </summary>
    /// <param name="context">当前授权处理上下文</param>
    /// <param name="httpContext">当前请求上下文</param>
    /// <param name="expiredTime">令牌过期时间</param>
    /// <param name="tokenPrefix">请求头中位于令牌之前的前缀</param>
    /// <param name="clockSkew">允许同一刷新令牌重复提交的兼容容差（秒）；默认 0，禁止重放。</param>
    /// <returns>当前身份有效、允许匿名或刷新成功时返回 <see langword="true"/>，表示可以继续后续授权；身份校验或刷新失败时返回 <see langword="false"/></returns>
    public static async Task<bool> AutoRefreshTokenAsync(AuthorizationHandlerContext context, HttpContext httpContext,
        long? expiredTime = null, string tokenPrefix = "Bearer ", long? clockSkew = null)
    {
        if (context == null || httpContext == null)
        {
            return false;
        }

        // 如果验证有效，则跳过刷新
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // 禁止使用刷新 Token 进行单独校验
            if (RefreshTokenClaims.All(k => context.User.Claims.Any(c => c.Type == k)))
            {
                return false;
            }

            if (httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                return true;
            }

            // 判断是否开启验证 AccessToken
            if (Penetrates.JWTSettings?.ValidateAccessToken == true)
            {
                // 读取 Token
                string accessToken = GetJwtBearerToken(httpContext, tokenPrefix: tokenPrefix);
                if (string.IsNullOrWhiteSpace(accessToken)
                    && httpContext.GetEndpoint()?.Metadata.GetMetadata<HubMetadata>() != null)
                {
                    accessToken = httpContext.Request.Query["access_token"].ToString();
                }

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    return false;
                }

                // 判断这个 Token 是否已标记过期
                IDistributedCache distributedCache = httpContext.RequestServices.GetService<IDistributedCache>();

                string cachedValue = distributedCache == null
                    ? null
                    : await GetTokenCacheValueAsync(distributedCache, AccessTokenBlacklistCacheKeyPrefix, accessToken,
                            httpContext.RequestAborted)
                        .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(cachedValue))
                {
                    return false;
                }
            }

            return true;
        }

        if (httpContext.GetEndpoint()?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            return true;
        }

        // 获取过期 Token 和 刷新 Token
        string expiredToken = GetJwtBearerToken(httpContext, tokenPrefix: tokenPrefix);
        string refreshToken = GetJwtBearerToken(httpContext, "X-Authorization", tokenPrefix);
        if (string.IsNullOrWhiteSpace(expiredToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        // 后续授权处理器持有同一个 Principal；只支持可以原地更新的标准身份集合。
        // 自定义不可变 Principal 在消费刷新令牌之前失败，不使用反射修改框架私有字段。
        if (context.User.GetType() != typeof(ClaimsPrincipal)
            || context.User.Identities is not IList<ClaimsIdentity> identities
            || identities.IsReadOnly)
        {
            return false;
        }

        // 交换新的 Token
        string newAccessToken = await ExchangeAsync(httpContext, expiredToken, refreshToken, expiredTime, clockSkew)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            return false;
        }

        // 按 Bearer 的声明映射配置重新建立已验证身份，保留后续角色/声明要求的语义。
        JwtBearerOptions bearerOptions = httpContext.RequestServices.GetService<IOptionsMonitor<JwtBearerOptions>>()
            ?.Get(JwtBearerDefaults.AuthenticationScheme);
        var tokenHandler = new JsonWebTokenHandler {MapInboundClaims = bearerOptions?.MapInboundClaims ?? true};
        TokenValidationResult validationResult = await ValidateTokenAsync(tokenHandler, newAccessToken,
                bearerOptions?.TokenValidationParameters ?? CreateTokenValidationParameters(Penetrates.JWTSettings))
            .ConfigureAwait(false);
        if (!validationResult.IsValid || validationResult.ClaimsIdentity == null)
        {
            return false;
        }

        // JWT Bearer 不支持 SignInAsync；更新原 Principal 后再继续组合策略验证。
        identities.Clear();
        identities.Add(validationResult.ClaimsIdentity);
        httpContext.User = context.User;

        string accessTokenKey = "access-token",
            xAccessTokenKey = "x-access-token",
            accessControlExposeKey = "Access-Control-Expose-Headers";

        // 返回新的 Token
        httpContext.Response.Headers[accessTokenKey] = newAccessToken;
        // 返回新的 刷新 Token
        httpContext.Response.Headers[xAccessTokenKey] = GenerateRefreshToken(newAccessToken);

        // 包含凭据的响应禁止被浏览器、代理或网关缓存
        httpContext.Response.Headers["Cache-Control"] = "no-store";
        httpContext.Response.Headers["Pragma"] = "no-cache";

        // 处理 axios 问题
        httpContext.Response.Headers.TryGetValue(accessControlExposeKey, out StringValues aches);
        httpContext.Response.Headers[accessControlExposeKey] = string.Join(',',
            StringValues.Concat(aches, new StringValues([accessTokenKey, xAccessTokenKey]))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        return true;
    }
}
