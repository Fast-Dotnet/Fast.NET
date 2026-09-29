// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Linq.Expressions;


// ReSharper disable once CheckNamespace
namespace System.Linq;

/// <summary>
/// IEnumerable 扩展
/// </summary>
[SuppressSniffer]
public static class IEnumerableExtension
{
    /// <summary>
    /// 根据条件成立再构建 OrderBy 排序
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="keySelector">用于生成排序键的委托</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <typeparam name="TKey">泛型类型</typeparam>
    /// <returns>根据条件成立再构建 OrderBy 排序</returns>
    public static IQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> sources, bool condition,
        Expression<Func<TSource, TKey>> keySelector)
    {
        return condition ? sources.OrderBy(keySelector) : sources;
    }

    /// <summary>
    /// 根据条件成立再构建 OrderByDescending 排序
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="keySelector">用于生成排序键的委托</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <typeparam name="TKey">泛型类型</typeparam>
    /// <returns>根据条件成立再构建 OrderByDescending 排序</returns>
    public static IQueryable<TSource> OrderByDescending<TSource, TKey>(this IQueryable<TSource> sources, bool condition,
        Expression<Func<TSource, TKey>> keySelector)
    {
        return condition ? sources.OrderByDescending(keySelector) : sources;
    }

    /// <summary>
    /// 根据条件成立再构建 Where 查询
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再构建 Where 查询</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, bool condition,
        Expression<Func<TSource, bool>> expression)
    {
        return condition ? sources.Where(expression) : sources;
    }

    /// <summary>
    /// 根据条件成立再构建 Where 查询，支持索引器
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再构建 Where 查询，支持索引器</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources, bool condition,
        Expression<Func<TSource, int, bool>> expression)
    {
        return condition ? sources.Where(expression) : sources;
    }

    /// <summary>
    /// 与操作合并多个表达式
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="expressions">要组合的表达式集合</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>与操作合并多个表达式</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources,
        params Expression<Func<TSource, bool>>[] expressions)
    {
        if (expressions == null || !expressions.Any())
        {
            return sources;
        }

        if (expressions.Length == 1)
        {
            return sources.Where(expressions[0]);
        }

        Expression<Func<TSource, bool>> expression = LinqExpression.Or<TSource>();
        foreach (Expression<Func<TSource, bool>> _expression in expressions)
        {
            expression = expression.Or(_expression);
        }

        return sources.Where(expression);
    }

    /// <summary>
    /// 与操作合并多个表达式，支持索引器
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="expressions">要组合的表达式集合</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>与操作合并多个表达式，支持索引器</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources,
        params Expression<Func<TSource, int, bool>>[] expressions)
    {
        if (expressions == null || !expressions.Any())
        {
            return sources;
        }

        if (expressions.Length == 1)
        {
            return sources.Where(expressions[0]);
        }

        Expression<Func<TSource, int, bool>> expression = LinqExpression.IndexOr<TSource>();
        foreach (Expression<Func<TSource, int, bool>> _expression in expressions)
        {
            expression = expression.Or(_expression);
        }

        return sources.Where(expression);
    }

    /// <summary>
    /// 根据条件成立再构建 WhereOr 查询
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="conditionExpressions">按条件参与组合的表达式集合</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再构建 WhereOr 查询</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources,
        params (bool condition, Expression<Func<TSource, bool>> expression)[] conditionExpressions)
    {
        var expressions = new List<Expression<Func<TSource, bool>>>();
        foreach ((bool condition, Expression<Func<TSource, bool>> expression) in conditionExpressions)
        {
            if (condition)
            {
                expressions.Add(expression);
            }
        }

        return sources.Where(expressions.ToArray());
    }

    /// <summary>
    /// 根据条件成立再构建 WhereOr 查询，支持索引器
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="conditionExpressions">按条件参与组合的表达式集合</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再构建 WhereOr 查询，支持索引器</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> sources,
        params (bool condition, Expression<Func<TSource, int, bool>> expression)[] conditionExpressions)
    {
        var expressions = new List<Expression<Func<TSource, int, bool>>>();
        foreach ((bool condition, Expression<Func<TSource, int, bool>> expression) in conditionExpressions)
        {
            if (condition)
            {
                expressions.Add(expression);
            }
        }

        return sources.Where(expressions.ToArray());
    }

    /// <summary>
    /// 根据条件成立再构建 Where 查询
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再构建 Where 查询集合</returns>
    public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> sources, bool condition,
        Func<TSource, bool> expression)
    {
        return condition ? sources.Where(expression) : sources;
    }

    /// <summary>
    /// 根据条件成立再构建 Where 查询，支持索引器
    /// </summary>
    /// <param name="sources">要查询或组合的源序列</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再构建 Where 查询，支持索引器集合</returns>
    public static IEnumerable<TSource> Where<TSource>(this IEnumerable<TSource> sources, bool condition,
        Func<TSource, int, bool> expression)
    {
        return condition ? sources.Where(expression) : sources;
    }
}
