// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Diagnostics;
using System.Text;

namespace Fast.NET.Core;

/// <summary>
/// 系统 Shell 工具类
/// </summary>
[SuppressSniffer]
public static class ShellUtil
{
    /// <summary>
    /// Linux Bash 命令
    /// </summary>
    /// <param name="command">要执行的命令文本</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>Linux Bash 命令</returns>
    public static string Bash(string command, int timeout = 0)
    {
        string escapedArgs = command.Replace("\"", "\\\"");
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            // 执行的命令
            FileName = "/bin/bash",
            Arguments = $"-c \"{escapedArgs}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // 不使用操作系统外壳程序来启动进程
            UseShellExecute = false,
            // 不创建窗口
            CreateNoWindow = true,
            // 输出编码
            StandardOutputEncoding = Encoding.UTF8,
            // 错误输出编码
            StandardErrorEncoding = Encoding.UTF8
        };

        process.Start();

        // 异步读取 stdout 和 stderr，防止缓冲区满导致死锁
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        if (timeout > 0)
        {
            bool exited = process.WaitForExit(timeout);

            // 检查是否超时或失败
            if (!exited)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch
                {
                    // 进程可能在 WaitForExit 超时后自行退出，此时无需重复终止
                }

                throw new TimeoutException("命令执行超时");
            }
        }

        string output = stdoutTask.GetAwaiter().GetResult();
        string error = stderrTask.GetAwaiter().GetResult();

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"命令执行出错: {error}");
        }

        return output;
    }

    /// <summary>
    /// Windows Cmd 命令
    /// </summary>
    /// <param name="command">要执行的命令文本</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>Windows Cmd 命令</returns>
    public static string Cmd(string command, string args = null, int timeout = 0)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            // 执行的命令
            FileName = command,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // 不使用操作系统外壳程序来启动进程
            UseShellExecute = false,
            // 不创建窗口
            CreateNoWindow = true,
            // 输出编码
            StandardOutputEncoding = Encoding.UTF8,
            // 错误输出编码
            StandardErrorEncoding = Encoding.UTF8
        };

        process.Start();

        // 异步读取 stdout 和 stderr，防止缓冲区满导致死锁
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        if (timeout > 0)
        {
            bool exited = process.WaitForExit(timeout);

            // 检查是否超时或失败
            if (!exited)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit();
                }
                catch
                {
                    // 进程可能在 WaitForExit 超时后自行退出，此时无需重复终止
                }

                throw new TimeoutException("命令执行超时");
            }
        }

        string output = stdoutTask.GetAwaiter().GetResult();
        string error = stderrTask.GetAwaiter().GetResult();

        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"命令执行出错: {error}");
        }

        return output;
    }
}
