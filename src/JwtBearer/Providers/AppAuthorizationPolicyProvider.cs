// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Authorization;

namespace Fast.JwtBearer;

/// <summary>
/// 授权策略提供器
/// </summary>
internal sealed class AppAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    internal const string FallbackServiceKey = "Fast.JwtBearer.AuthorizationPolicyFallback";

    /// <summary>
    /// 加入 Fast 前已注册的策略提供器；命名、默认和回退策略仍由它维护。
    /// </summary>
    public IAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public AppAuthorizationPolicyProvider(
        [Microsoft.Extensions.DependencyInjection.FromKeyedServices(FallbackServiceKey)]
        IAuthorizationPolicyProvider fallbackPolicyProvider)
    {
        FallbackPolicyProvider = fallbackPolicyProvider;
    }

    /// <inheritdoc />
    public async Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        AuthorizationPolicy policy = await FallbackPolicyProvider.GetDefaultPolicyAsync();
        // 默认授权保留原 requirements，并加入 Fast 权限入口，避免借用其他处理器的要求。
        if (policy.Requirements.OfType<AppAuthorizeRequirement>().Any())
        {
            return policy;
        }

        return new AuthorizationPolicyBuilder(policy).AddRequirements(new AppAuthorizeRequirement()).Build();
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetFallbackPolicyAsync()
    {
        return FallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        // 判断是否是包含授权策略前缀
        if (policyName.StartsWith("<Fast.JwtBearer.AppAuthorizeRequirement>", StringComparison.Ordinal))
        {
            // 解析策略名并获取策略参数
            string[] policies = policyName["<Fast.JwtBearer.AppAuthorizeRequirement>".Length..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            // 添加策略需求
            AuthorizationPolicyBuilder policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
            policy.AddRequirements(new AppAuthorizeRequirement(policies));

            return Task.FromResult(policy.Build());
        }

        // 如果策略不匹配，则返回回退策略
        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
