// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Fast.Logging;

/// <summary>
/// <see cref="ILoggingBuilder"/> 扩展
/// </summary>
internal static class ILoggingBuilderExtension
{
    /// <summary>
    /// 添加控制台默认格式化器
    /// </summary>
    /// <param name="builder">日志服务构建器</param>
    /// <returns>添加控制台默认格式化器</returns>
    internal static ILoggingBuilder AddConsoleFormatter(this ILoggingBuilder builder)
    {
        builder.AddConsole(options => { options.FormatterName = Penetrates.ConsoleFormatterName; })
            .AddConsoleFormatter<ConsoleFormatterExtend, ConsoleFormatterExtendOptions>(options =>
            {
                options.DateFormat = "yyyy-MM-dd HH:mm:ss.fffffff zzz dddd";
                options.ColorBehavior = LoggerColorBehavior.Default;
            });

        return builder;
    }
}
