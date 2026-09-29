// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.EventBus;

/// <summary>
/// 事件处理程序特性
/// </summary>
/// <remarks>
/// <para>作用于 <see cref="IEventSubscriber"/> 实现类实例方法</para>
/// <para>支持多个事件Id触发同一个事件处理程序</para>
/// </remarks>
[SuppressSniffer]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class EventSubscribeAttribute : Attribute
{
    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <remarks>只支持事件类型和 Enum 类型</remarks>
    /// <param name="eventId">日志事件标识</param>
    public EventSubscribeAttribute(object eventId)
    {
        if (eventId is string eventIdStr)
        {
            EventId = eventIdStr;
        }
        else if (eventId is Enum eventIdEnum)
        {
            EventId = eventIdEnum.EventBusToString();
        }
        else
        {
            throw new ArgumentException("Only support string or Enum data type.");
        }
    }

    /// <summary>
    /// 事件Id
    /// </summary>
    public string EventId { get; set; }

    /// <summary>
    /// 是否启用执行完成触发 GC 回收
    /// </summary>
    /// <remarks>类型，默认关闭；通常应交由运行时自行决定垃圾回收时机</remarks>
    public object GCCollect { get; set; } = false;

    /// <summary>
    /// 重试次数
    /// </summary>
    public int NumRetries { get; set; } = 0;

    /// <summary>
    /// 重试间隔时间
    /// </summary>
    /// <remarks>默认 1000 毫秒</remarks>
    public int RetryTimeout { get; set; } = 1000;

    /// <summary>
    /// 可以指定特定异常类型才重试
    /// </summary>
    public Type[] ExceptionTypes { get; set; }

    /// <summary>
    /// 重试失败策略配置
    /// </summary>
    public Type FallbackPolicy { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    /// <remarks>数值越大的先执行</remarks>
    public int Order { get; set; } = 0;
}
