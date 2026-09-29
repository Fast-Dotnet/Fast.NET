// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fast.IaaS;

/// <summary>
/// 重试静态类
/// </summary>
public sealed class RetryUtil
{
    /// <summary>
    /// 重试有异常的方法，还可以指定特定异常
    /// </summary>
    /// <param name="action">要执行的操作委托</param>
    /// <param name="numRetries">首次调用之后最多再尝试的次数；小于等于 0 时直接执行一次，不使用重试或降级策略。</param>
    /// <param name="retryTimeout">两次重试之间的等待时间</param>
    /// <param name="finalThrow">失败不再重试时是否继续抛出原异常；默认 true。</param>
    /// <param name="exceptionTypes">允许触发重试的异常类型集合</param>
    /// <param name="fallbackPolicy">重试耗尽或异常类型不匹配时使用的降级策略</param>
    /// <param name="retryAction">每次失败后执行的重试回调</param>
    public static void Invoke(Action action, int numRetries, int retryTimeout = 1000, bool finalThrow = true,
        Type[] exceptionTypes = null, Action<Exception> fallbackPolicy = null, Action<int, int> retryAction = null)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        InvokeAsync(async () =>
            {
                action();
                await Task.CompletedTask;
            }, numRetries, retryTimeout, finalThrow, exceptionTypes, async ex =>
            {
                fallbackPolicy?.Invoke(ex);
                await Task.CompletedTask;
            }, async (total, times) =>
            {
                retryAction?.Invoke(total, times);
                await Task.CompletedTask;
            })
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 重试有异常的方法，还可以指定特定异常
    /// </summary>
    /// <param name="action">要执行的操作委托</param>
    /// <param name="numRetries">首次调用之后最多再尝试的次数；小于等于 0 时直接执行一次，不使用重试或降级策略。</param>
    /// <param name="retryTimeout">两次重试之间的等待时间</param>
    /// <param name="finalThrow">失败不再重试时是否继续抛出原异常；默认 true。</param>
    /// <param name="exceptionTypes">允许触发重试的异常类型集合</param>
    /// <param name="fallbackPolicy">重试耗尽或异常类型不匹配时使用的降级策略</param>
    /// <param name="retryAction">每次失败后执行的重试回调</param>
    /// <param name="cancellationToken">调用方取消令牌；检测到取消时不执行降级策略，也不受 finalThrow 开关影响。操作委托内部须自行观察该令牌。</param>
    /// <returns>操作成功或允许的失败降级完成时结束的任务；未处理的异常及检测到的调用方取消继续传播。</returns>
    public static async Task InvokeAsync(Func<Task> action, int numRetries, int retryTimeout = 1000, bool finalThrow = true,
        Type[] exceptionTypes = null, Func<Exception, Task> fallbackPolicy = null, Func<int, int, Task> retryAction = null,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        // 未配置重试次数时只执行一次，不进入重试循环
        if (numRetries <= 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await action().ConfigureAwait(false);
            return;
        }

        // 存储总的重试次数
        int totalNumRetries = numRetries;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await action().ConfigureAwait(false);
                break;
            }
            catch (Exception ex)
            {
                // 调用方取消优先于重试与降级，不能因 finalThrow=false 被转换为成功。
                cancellationToken.ThrowIfCancellationRequested();
                bool retriesExhausted = --numRetries < 0;
                bool cannotRetryException = exceptionTypes != null
                                            && exceptionTypes.Length > 0
                                            && !exceptionTypes.Where(u => u != null).Any(u => u.IsAssignableFrom(ex.GetType()));

                // 重试耗尽或异常类型不匹配时统一执行失败回调
                if (retriesExhausted || cannotRetryException)
                {
                    if (fallbackPolicy != null)
                    {
                        await fallbackPolicy.Invoke(ex).ConfigureAwait(false);
                    }

                    if (finalThrow)
                    {
                        throw;
                    }

                    return;
                }

                // 重试调用委托
                if (retryAction != null)
                {
                    await retryAction.Invoke(totalNumRetries, totalNumRetries - numRetries).ConfigureAwait(false);
                }

                // 仅对允许重试的异常等待指定间隔后重试
                if (retryTimeout > 0)
                {
                    await Task.Delay(retryTimeout, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
