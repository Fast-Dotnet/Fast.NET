// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fast.Logging;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供动态 API 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 注册日志服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：Logging:Fast</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddLoggingService(this IServiceCollection services, IConfiguration configuration,
        string section = "Logging:Fast")
    {
        Debugging.Info("Registering logging......");

        services.AddConfigurableOptions<LoggingSettingsOptions>(section);

        LoggingSettingsOptions loggingSettings = configuration
            .GetSection(section)
            .Get<LoggingSettingsOptions>()
            .LoadPostConfigure();

        // 存储服务提供器
        Penetrates.InternalServices = services;

        services.AddLogging(loggingBuilder =>
        {
            // 添加控制台默认格式化器
            loggingBuilder.AddConsoleFormatter();

            // 根据默认日志级别创建对应的文件日志
            if (LogLevel.Trace >= loggingSettings.MiniLogLevel)
            {
                loggingBuilder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
                    new FileLoggerProvider($"logs/trace{loggingSettings.FileFormat}.log",
                        GetLogOptions(LogLevel.Trace, loggingSettings.FileSizeLimit!.Value))));
            }

            if (LogLevel.Debug >= loggingSettings.MiniLogLevel)
            {
                loggingBuilder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
                    new FileLoggerProvider($"logs/debug{loggingSettings.FileFormat}.log",
                        GetLogOptions(LogLevel.Debug, loggingSettings.FileSizeLimit!.Value))));
            }

            if (LogLevel.Information >= loggingSettings.MiniLogLevel)
            {
                loggingBuilder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
                    new FileLoggerProvider($"logs/information{loggingSettings.FileFormat}.log",
                        GetLogOptions(LogLevel.Information, loggingSettings.FileSizeLimit!.Value))));
            }

            if (LogLevel.Warning >= loggingSettings.MiniLogLevel)
            {
                loggingBuilder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
                    new FileLoggerProvider($"logs/warning{loggingSettings.FileFormat}.log",
                        GetLogOptions(LogLevel.Warning, loggingSettings.FileSizeLimit!.Value))));
            }

            if (LogLevel.Error >= loggingSettings.MiniLogLevel)
            {
                loggingBuilder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
                    new FileLoggerProvider($"logs/error{loggingSettings.FileFormat}.log",
                        GetLogOptions(LogLevel.Error, loggingSettings.FileSizeLimit!.Value))));
            }

            if (LogLevel.Critical >= loggingSettings.MiniLogLevel && loggingSettings.EnableCritical!.Value)
            {
                loggingBuilder.Services.Add(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(_ =>
                    new FileLoggerProvider($"logs/critical{loggingSettings.FileFormat}.log",
                        GetLogOptions(LogLevel.Critical, loggingSettings.FileSizeLimit!.Value))));
            }
        });

        // 注册 Logging Startup 过滤器
        services.AddTransient(typeof(IStartupFilter), typeof(LoggingStartupFilter));

        return services;
    }

    /// <summary>
    /// 配置日志
    /// </summary>
    /// <param name="logLevel">日志级别</param>
    /// <param name="fileSizeLimitBytes">日志文件大小 控制每一个日志文件最大存储大小，默认无限制，单位是 B，也就是 1024 才等于 1KB</param>
    /// <returns>配置日志</returns>
    private static FileLoggerOptions GetLogOptions(LogLevel logLevel, long fileSizeLimitBytes)
    {
        return new FileLoggerOptions
        {
            IncludeScopes = true,
            WithTraceId = true,
            WithStackFrame = true,
            FileNameRule = fileName => string.Format(fileName, DateTime.Now, logLevel.ToString()),
            WriteFilter = logMsg => logMsg.LogLevel == logLevel,
            FileSizeLimitBytes = fileSizeLimitBytes,
            MessageFormat = logMsg =>
            {
                var msg = new List<string>
                {
                    $"{logMsg.LogName}",
                    $"##日志时间## {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}",
                    $"##日志等级## {logLevel}",
                    $"##日志内容## {logMsg.Message}"
                };
                if (!string.IsNullOrEmpty(logMsg.Exception?.ToString()))
                {
                    msg.Add($"##异常信息## {logMsg.Exception}");
                }

                // Generating template strings
                string template = TP.Wrapper("Fast.NET", "", msg.ToArray());
                return template;
            },
            HandleWriteError = err =>
            {
                err.UseRollbackFileName(Path.Combine(Path.GetDirectoryName(err.CurrentFileName) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(err.CurrentFileName) + "_alt" + Path.GetExtension(err.CurrentFileName)));
            }
        };
    }
}
