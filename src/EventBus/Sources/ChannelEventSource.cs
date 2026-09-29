// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json.Serialization;

namespace Fast.EventBus;

/// <summary>
/// 内存通道事件源（事件承载对象）
/// </summary>
[SuppressSniffer]
public sealed class ChannelEventSource : IEventSource
{
    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    public ChannelEventSource()
    {
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    public ChannelEventSource(string eventId)
    {
        EventId = eventId;
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="payload">要写入令牌的载荷</param>
    public ChannelEventSource(string eventId, object payload) : this(eventId)
    {
        Payload = payload;
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="payload">要写入令牌的载荷</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    public ChannelEventSource(string eventId, object payload, CancellationToken cancellationToken) : this(eventId, payload)
    {
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    public ChannelEventSource(Enum eventId) : this(eventId.EventBusToString())
    {
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="payload">要写入令牌的载荷</param>
    public ChannelEventSource(Enum eventId, object payload) : this(eventId.EventBusToString(), payload)
    {
    }

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventId">日志事件标识</param>
    /// <param name="payload">要写入令牌的载荷</param>
    /// <param name="cancellationToken">用于取消异步操作的令牌</param>
    public ChannelEventSource(Enum eventId, object payload, CancellationToken cancellationToken) : this(
        eventId.EventBusToString(), payload, cancellationToken)
    {
    }

    /// <inheritdoc />
    public string EventId { get; set; }

    /// <inheritdoc />
    public object Payload { get; set; }

    /// <inheritdoc />
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; }
}
