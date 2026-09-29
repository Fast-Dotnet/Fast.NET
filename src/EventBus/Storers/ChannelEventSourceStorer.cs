// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Threading.Channels;

namespace Fast.EventBus;

/// <summary>
/// 内存通道事件源存储器（默认实现）
/// </summary>
/// <remarks>
/// <para>顾名思义，这里指的是事件消息存储中心，提供读写能力</para>
/// <para>默认实现为内存中的 <see cref="System.Threading.Channels.Channel"/></para>
/// </remarks>
internal sealed class ChannelEventSourceStorer : IEventSourceStorer
{
    /// <summary>
    /// 内存通道事件源存储器
    /// </summary>
    private readonly Channel<IEventSource> _channel;

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="capacity">管道最多能够处理多少消息，超过该容量进入等待写入</param>
    public ChannelEventSourceStorer(int capacity)
    {
        // 配置通道，设置超出默认容量后进入等待
        var boundedChannelOptions = new BoundedChannelOptions(capacity) {FullMode = BoundedChannelFullMode.Wait};

        // 创建有限容量通道
        _channel = Channel.CreateBounded<IEventSource>(boundedChannelOptions);
    }

    /// <inheritdoc />
    public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
    {
        if (eventSource == null)
        {
            throw new ArgumentNullException(nameof(eventSource));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (EventDispatchScope.IsDispatching(this))
        {
            // 处理器等待满队列会与消费者相互等待；保留有界背压，对重入的满队列明确失败而不丢弃事件。
            if (!_channel.Writer.TryWrite(eventSource))
            {
                throw new InvalidOperationException("事件处理器不能等待写入同一个已满队列。请在当前处理完成后发布，或显式处理容量不足。");
            }

            return;
        }

        await _channel.Writer.WriteAsync(eventSource, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
    {
        // 读取一条事件源
        IEventSource eventSource = await _channel.Reader.ReadAsync(cancellationToken);
        return eventSource;
    }
}
