// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Consul;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;

namespace Fast.Consul;

/// <inheritdoc cref="IConsulRegister" />
internal sealed class ConsulRegister : IConsulRegister
{
    private readonly IServer _server;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ConsulSettingsOptions _consulSettingsOptions;

    public ConsulRegister(IServer server, IWebHostEnvironment webHostEnvironment,
        IOptionsMonitor<ConsulSettingsOptions> consulSettingsOptions)
    {
        _server = server;
        _webHostEnvironment = webHostEnvironment;
        _consulSettingsOptions = consulSettingsOptions.CurrentValue;
    }

    /// <inheritdoc />
    public async Task ConsulRegisterAsync()
    {
        using var client = new ConsulClient(options =>
        {
            // Consul 客户端地址
            options.Address = new Uri(_consulSettingsOptions.Address);
        });

        // 获取当前程序启动的地址
        string startupAddress = string.IsNullOrWhiteSpace(_consulSettingsOptions.ServiceAddress)
            ? _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
            : _consulSettingsOptions.ServiceAddress;
        if (!Uri.TryCreate(startupAddress, UriKind.Absolute, out Uri startupUri))
        {
            throw new InvalidOperationException("无法从服务器功能中获取有效的应用监听地址，Consul 服务注册已终止。");
        }

        if (startupUri.Host is "0.0.0.0" or "::" or "[::]")
        {
            throw new InvalidOperationException("应用监听的是通配地址，无法直接注册到 Consul；请配置 ConsulSettings:ServiceAddress。");
        }

        // 服务名携带入口程序集版本，便于不同版本在 Consul 中并行注册
        string version = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        string[] versionArr = version?.Split('.');

        // Consul 服务版本统一到 major.minor.patch，忽略第四段副修订版本号
        if (versionArr?.Length >= 4)
        {
            version = $"{versionArr[0]}.{versionArr[1]}.{versionArr[2]}";
        }

        var registration = new AgentServiceRegistration
        {
            // 唯一Id
            ID = Guid.NewGuid().ToString("N"),
            // 服务名
            Name = _webHostEnvironment.ApplicationName + $"{(string.IsNullOrEmpty(version) ? null : $"_v{version}")}",
            // 服务绑定 IP
            Address = startupUri.Host,
            // 服务绑定端口
            Port = startupUri.Port,
            // Tag 标签
            Check = new AgentServiceCheck
            {
                // 服务启动后多久注册
                DeregisterCriticalServiceAfter =
                    TimeSpan.FromSeconds(_consulSettingsOptions.DeregisterCriticalServiceAfter!.Value),
                // 健康检查时间间隔
                Interval = TimeSpan.FromSeconds(_consulSettingsOptions.HealthCheckInterval!.Value),
                // 健康检查地址
                HTTP = $"{startupUri.AbsoluteUri.TrimEnd('/')}{_consulSettingsOptions.HealthCheck}",
                // 健康检查超时时间
                Timeout = TimeSpan.FromSeconds(_consulSettingsOptions.HealthCheckTimeout!.Value)
            }
        };

        await client.Agent.ServiceRegister(registration).ConfigureAwait(false);
    }
}
