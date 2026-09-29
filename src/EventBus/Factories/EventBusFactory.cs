// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;

namespace Fast.EventBus;

/// <inheritdoc cref="IEventBusFactory" />
internal sealed class EventBusFactory : IEventBusFactory
{
    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorer _eventSourceStorer;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventSourceStorer">事件源存储器</param>
    public EventBusFactory(IEventSourceStorer eventSourceStorer)
    {
        _eventSourceStorer = eventSourceStorer;
    }

    /// <inheritdoc />
    public async Task Subscribe(string eventId, Func<EventHandlerExecutingContext, Task> handler,
        EventSubscribeAttribute attribute = null, MethodInfo handlerMethod = null, CancellationToken cancellationToken = default)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        await _eventSourceStorer.WriteAsync(
            new EventSubscribeOperateSource
            {
                SubscribeEventId = eventId,
                Attribute = attribute,
                Handler = handler,
                HandlerMethod = handlerMethod,
                Operate = EventSubscribeOperates.Append
            }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Unsubscribe(string eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == null)
        {
            throw new ArgumentNullException(nameof(eventId));
        }

        await _eventSourceStorer.WriteAsync(
            new EventSubscribeOperateSource {SubscribeEventId = eventId, Operate = EventSubscribeOperates.Remove},
            cancellationToken);
    }
}
