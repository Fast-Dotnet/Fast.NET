// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.EventBus;

/// <summary>
/// 识别正在向同一有界队列重入发布的处理器调用链。
/// </summary>
internal sealed class EventDispatchScope : IDisposable
{
    private static readonly AsyncLocal<EventDispatchScope> Current = new();
    private readonly EventDispatchScope _parent;
    private readonly IEventSourceStorer _storer;
    private volatile bool _active = true;

    private EventDispatchScope(IEventSourceStorer storer)
    {
        _parent = Current.Value;
        _storer = storer;
        Current.Value = this;
    }

    internal static EventDispatchScope Enter(IEventSourceStorer storer)
    {
        return new EventDispatchScope(storer);
    }

    internal static bool IsDispatching(IEventSourceStorer storer)
    {
        for (EventDispatchScope scope = Current.Value; scope != null; scope = scope._parent)
        {
            if (scope._active && ReferenceEquals(scope._storer, storer))
            {
                return true;
            }
        }

        return false;
    }

    public void Dispose()
    {
        _active = false;
        Current.Value = _parent;
    }
}
