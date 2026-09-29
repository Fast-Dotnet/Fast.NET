// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;

namespace Fast.EventBus;

/// <summary>
/// 事件处理程序上下文
/// </summary>
[SuppressSniffer]
public abstract class EventHandlerContext
{
    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="eventSource">事件源（事件承载对象）</param>
    /// <param name="properties">共享上下文数据</param>
    /// <param name="handlerMethod">触发的方法</param>
    /// <param name="attribute">订阅特性</param>
    internal EventHandlerContext(IEventSource eventSource, IDictionary<object, object> properties, MethodInfo handlerMethod,
        EventSubscribeAttribute attribute)
    {
        Source = eventSource;
        Properties = properties;
        HandlerMethod = handlerMethod;
        Attribute = attribute;
    }

    /// <summary>
    /// 事件源（事件承载对象）
    /// </summary>
    public IEventSource Source { get; }

    /// <summary>
    /// 共享上下文数据
    /// </summary>
    public IDictionary<object, object> Properties { get; set; }

    /// <summary>
    /// 触发的方法
    /// </summary>
    /// <remarks>如果是动态订阅，可能为 <see langword="null"/></remarks>
    public MethodInfo HandlerMethod { get; }

    /// <summary>
    /// 订阅特性
    /// </summary>
    /// <remarks><remarks>如果是动态订阅，可能为 <see langword="null"/></remarks></remarks>
    public EventSubscribeAttribute Attribute { get; }
}
