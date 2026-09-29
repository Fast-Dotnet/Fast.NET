// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel.DataAnnotations;
using Fast.Runtime;

namespace Fast.Consul;

/// <summary>
/// Consul 配置选项
/// </summary>
public sealed class ConsulSettingsOptions : IPostConfigure
{
    /// <summary>
    /// Consul 是否启用
    /// </summary>
    [Required]
    public bool? Enable { get; set; }

    /// <summary>
    /// Consul 客户端地址
    /// </summary>
    [Required]
    public string Address { get; set; }

    /// <summary>
    /// 对外注册的服务地址；为空时使用服务器实际监听地址
    /// </summary>
    /// <remarks>容器或反向代理场景应显式配置可被 Consul 访问的绝对地址</remarks>
    public string ServiceAddress { get; set; }

    /// <summary>
    /// Consul 健康检查地址
    /// </summary>
    [Required]
    public string HealthCheck { get; set; }

    /// <summary>
    /// Consul 服务启动后多久注册，单位为秒
    /// </summary>
    public int? DeregisterCriticalServiceAfter { get; set; }

    /// <summary>
    /// Consul 健康检查时间间隔，单位为秒
    /// </summary>
    public int? HealthCheckInterval { get; set; }

    /// <summary>
    /// Consul 健康检查超时时间，单位为秒
    /// </summary>
    public int? HealthCheckTimeout { get; set; }

    /// <inheritdoc />
    public void PostConfigure()
    {
        Enable ??= true;
        Address ??= "http://127.0.0.1:8500";
        HealthCheck ??= "/healthCheck";
        DeregisterCriticalServiceAfter ??= 60;
        HealthCheckInterval ??= 10;
        HealthCheckTimeout ??= 5;

        if (!Uri.TryCreate(Address, UriKind.Absolute, out Uri consulUri) || consulUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("ConsulSettings:Address 必须是有效的 HTTP/HTTPS 绝对地址。");
        }

        if (!string.IsNullOrWhiteSpace(ServiceAddress)
            && (!Uri.TryCreate(ServiceAddress, UriKind.Absolute, out Uri serviceUri)
                || serviceUri.Scheme is not ("http" or "https")))
        {
            throw new InvalidOperationException("ConsulSettings:ServiceAddress 必须是有效的 HTTP/HTTPS 绝对地址。");
        }

        if (string.IsNullOrWhiteSpace(HealthCheck))
        {
            throw new InvalidOperationException("ConsulSettings:HealthCheck 不能为空。");
        }

        if (!HealthCheck.StartsWith('/'))
        {
            HealthCheck = "/" + HealthCheck;
        }

        if (DeregisterCriticalServiceAfter <= 0 || HealthCheckInterval <= 0 || HealthCheckTimeout <= 0)
        {
            throw new InvalidOperationException("Consul 健康检查和服务摘除时间必须大于 0 秒。");
        }
    }
}
