// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.IO;

namespace Fast.IaaS;

/// <summary>
/// 控制台同步写入回调使用的颜色与文本输出上下文
/// </summary>
/// <remarks>仅在当前写入回调内使用，输出重定向时忽略颜色设置</remarks>
internal sealed class ConsoleWriter
{
    private readonly TextWriter _writer;
    private readonly bool _useColor;
    private readonly ConsoleColor _originalForegroundColor;
    private readonly ConsoleColor _originalBackgroundColor;

    internal ConsoleWriter(TextWriter writer)
    {
        _writer = writer;
        _useColor = !Console.IsOutputRedirected;
        _originalForegroundColor = Console.ForegroundColor;
        _originalBackgroundColor = Console.BackgroundColor;
    }

    /// <summary>
    /// 当前控制台前景色，输出重定向时忽略设置
    /// </summary>
    public ConsoleColor ForegroundColor
    {
        get => Console.ForegroundColor;
        set
        {
            if (_useColor)
            {
                Console.ForegroundColor = value;
            }
        }
    }

    /// <summary>
    /// 当前控制台背景色，输出重定向时忽略设置
    /// </summary>
    public ConsoleColor BackgroundColor
    {
        get => Console.BackgroundColor;
        set
        {
            if (_useColor)
            {
                Console.BackgroundColor = value;
            }
        }
    }

    /// <summary>
    /// 写入文本而不追加换行
    /// </summary>
    /// <param name="value">要写入的文本，为 <see langword="null"/> 时不输出内容</param>
    public void Write(string value)
    {
        _writer.Write(value);
    }

    /// <summary>
    /// 写入对象的文本表示而不追加换行
    /// </summary>
    /// <param name="value">要写入的对象，为 <see langword="null"/> 时不输出内容</param>
    public void Write(object value)
    {
        _writer.Write(value);
    }

    /// <summary>
    /// 写入换行
    /// </summary>
    public void WriteLine()
    {
        _writer.WriteLine();
    }

    /// <summary>
    /// 写入文本并追加换行
    /// </summary>
    /// <param name="value">要写入的文本，为 <see langword="null"/> 时仅输出换行</param>
    public void WriteLine(string value)
    {
        _writer.WriteLine(value);
    }

    /// <summary>
    /// 写入对象的文本并追加换行
    /// </summary>
    /// <param name="value">要写入的对象，为 <see langword="null"/> 时仅输出换行</param>
    public void WriteLine(object value)
    {
        _writer.WriteLine(value);
    }

    /// <summary>
    /// 恢复进入当前写入回调时的前景色和背景色
    /// </summary>
    public void ResetColor()
    {
        if (_useColor)
        {
            Console.ForegroundColor = _originalForegroundColor;
            Console.BackgroundColor = _originalBackgroundColor;
        }
    }
}
