// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.EventBus;

/// <summary>
/// 基于内存通道事件发布者（默认实现）
/// </summary>
internal sealed class ChannelEventPublisher : IEventPublisher
{
    /// <summary>
    /// 事件源存储器
    /// </summary>
    private readonly IEventSourceStorer _eventSourceStorer;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventSourceStorer">事件源存储器</param>
    public ChannelEventPublisher(IEventSourceStorer eventSourceStorer)
    {
        _eventSourceStorer = eventSourceStorer;
    }

    /// <inheritdoc />
    public async Task PublishAsync(IEventSource eventSource)
    {
        ArgumentNullException.ThrowIfNull(eventSource);

        await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishDelayAsync(IEventSource eventSource, long delay)
    {
        ArgumentNullException.ThrowIfNull(eventSource);
        if (delay < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "延迟时间不能为负数。");
        }

        // 必须等待延迟与入队完成，确保取消和写入异常能够由调用方观察到
        await Task.Delay(TimeSpan.FromMilliseconds(delay), eventSource.CancellationToken);

        await _eventSourceStorer.WriteAsync(eventSource, eventSource.CancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync(string eventId, object payload = null, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new ChannelEventSource(eventId, payload, cancellationToken));
    }

    /// <inheritdoc />
    public async Task PublishAsync(Enum eventId, object payload = null, CancellationToken cancellationToken = default)
    {
        await PublishAsync(new ChannelEventSource(eventId, payload, cancellationToken));
    }

    /// <inheritdoc />
    public async Task PublishDelayAsync(string eventId, long delay, object payload = null,
        CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new ChannelEventSource(eventId, payload, cancellationToken), delay);
    }

    /// <inheritdoc />
    public async Task PublishDelayAsync(Enum eventId, long delay, object payload = null,
        CancellationToken cancellationToken = default)
    {
        await PublishDelayAsync(new ChannelEventSource(eventId, payload, cancellationToken), delay);
    }
}
