// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fast.Logging;

/// <summary>
/// 常量、公共方法配置类
/// </summary>
internal static class Penetrates
{
    /// <summary>
    /// 应用服务
    /// </summary>
    internal static IServiceCollection InternalServices;

    /// <summary>
    /// 根服务
    /// </summary>
    internal static IServiceProvider RootServices;

    /// <summary>
    /// 请求上下文
    /// </summary>
    internal static HttpContext HttpContext =>
        MAppContext.CatchOrDefault(() => RootServices?.GetService<IHttpContextAccessor>()?.HttpContext);

    /// <summary>
    /// 控制台默认格式化程序名称
    /// </summary>
    internal const string ConsoleFormatterName = "console-format";

    /// <summary>
    /// 设置日志上下文
    /// </summary>
    /// <param name="scopeProvider">用于读取当前日志作用域的提供器</param>
    /// <param name="logMsg">要格式化或输出的日志消息</param>
    /// <param name="includeScopes">是否在日志中包含作用域信息</param>
    /// <returns>设置日志上下文</returns>
    internal static LogMessage SetLogContext(IExternalScopeProvider scopeProvider, LogMessage logMsg, bool includeScopes)
    {
        if (includeScopes && scopeProvider != null)
        {
            // 解析日志上下文数据
            scopeProvider.ForEachScope<object>((scope, _) =>
            {
                if (scope != null && scope is LogContext context)
                {
                    if (logMsg.Context == null)
                    {
                        logMsg.Context = context;
                    }
                    else
                    {
                        logMsg.Context = logMsg.Context.SetRange(context.Properties);
                    }
                }
            }, null);
        }

        return logMsg;
    }
}
