// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;

namespace Fast.EventBus;

/// <summary>
/// 事件总线工厂
/// </summary>
[SuppressSniffer]
public interface IEventBusFactory
{
    /// <summary>
    /// 添加事件订阅者
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="handler">处理当前事件或请求的委托</param>
    /// <param name="attribute">EventSubscribeAttribute 特性对象</param>
    /// <param name="handlerMethod">事件处理器方法</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>表示异步“添加事件订阅者”操作的任务</returns>
    Task Subscribe(string eventId, Func<EventHandlerExecutingContext, Task> handler, EventSubscribeAttribute attribute = null,
        MethodInfo handlerMethod = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除事件订阅者
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    /// <returns>表示异步“删除事件订阅者”操作的任务</returns>
    Task Unsubscribe(string eventId, CancellationToken cancellationToken = default);
}
