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
/// 提供表达式扩展方法
/// </summary>
[SuppressSniffer]
public static class ExpressionExtension
{
    /// <summary>
    /// 组合两个表达式
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="extendExpression">用于组合两个表达式的表达式</param>
    /// <param name="mergeWay">两个表达式的组合方式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>组合两个表达式</returns>
    public static Expression<TSource> Compose<TSource>(this Expression<TSource> expression, Expression<TSource> extendExpression,
        Func<Expression, Expression, Expression> mergeWay)
    {
        var parameterExpressionSetter = expression.Parameters
            .Select((u, i) => new {u, Parameter = extendExpression.Parameters[i]})
            .ToDictionary(d => d.Parameter, d => d.u);

        Expression extendExpressionBody =
            ParameterReplaceExpressionVisitor.ReplaceParameters(parameterExpressionSetter, extendExpression.Body);
        return Expression.Lambda<TSource>(mergeWay(expression.Body, extendExpressionBody), expression.Parameters);
    }

    /// <summary>
    /// 与操作合并两个表达式
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="extendExpression">用于与操作合并两个表达式的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>与操作合并两个表达式</returns>
    public static Expression<Func<TSource, bool>> And<TSource>(this Expression<Func<TSource, bool>> expression,
        Expression<Func<TSource, bool>> extendExpression)
    {
        return expression.Compose(extendExpression, Expression.AndAlso);
    }

    /// <summary>
    /// 与操作合并两个表达式，支持索引器
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="extendExpression">用于与操作合并两个表达式，支持索引器的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>与操作合并两个表达式，支持索引器</returns>
    public static Expression<Func<TSource, int, bool>> And<TSource>(this Expression<Func<TSource, int, bool>> expression,
        Expression<Func<TSource, int, bool>> extendExpression)
    {
        return expression.Compose(extendExpression, Expression.AndAlso);
    }

    /// <summary>
    /// 根据条件成立再与操作合并两个表达式
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="extendExpression">用于根据条件成立再与操作合并两个表达式的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再与操作合并两个表达式</returns>
    public static Expression<Func<TSource, bool>> AndIf<TSource>(this Expression<Func<TSource, bool>> expression, bool condition,
        Expression<Func<TSource, bool>> extendExpression)
    {
        return condition ? expression.Compose(extendExpression, Expression.AndAlso) : expression;
    }

    /// <summary>
    /// 根据条件成立再与操作合并两个表达式，支持索引器
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="extendExpression">用于根据条件成立再与操作合并两个表达式，支持索引器的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再与操作合并两个表达式，支持索引器</returns>
    public static Expression<Func<TSource, int, bool>> AndIf<TSource>(this Expression<Func<TSource, int, bool>> expression,
        bool condition, Expression<Func<TSource, int, bool>> extendExpression)
    {
        return condition ? expression.Compose(extendExpression, Expression.AndAlso) : expression;
    }

    /// <summary>
    /// 或操作合并两个表达式
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="extendExpression">用于或操作合并两个表达式的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>或操作合并两个表达式</returns>
    public static Expression<Func<TSource, bool>> Or<TSource>(this Expression<Func<TSource, bool>> expression,
        Expression<Func<TSource, bool>> extendExpression)
    {
        return expression.Compose(extendExpression, Expression.OrElse);
    }

    /// <summary>
    /// 或操作合并两个表达式，支持索引器
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="extendExpression">用于或操作合并两个表达式，支持索引器的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>或操作合并两个表达式，支持索引器</returns>
    public static Expression<Func<TSource, int, bool>> Or<TSource>(this Expression<Func<TSource, int, bool>> expression,
        Expression<Func<TSource, int, bool>> extendExpression)
    {
        return expression.Compose(extendExpression, Expression.OrElse);
    }

    /// <summary>
    /// 根据条件成立再或操作合并两个表达式
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="extendExpression">用于根据条件成立再或操作合并两个表达式的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再或操作合并两个表达式</returns>
    public static Expression<Func<TSource, bool>> OrIf<TSource>(this Expression<Func<TSource, bool>> expression, bool condition,
        Expression<Func<TSource, bool>> extendExpression)
    {
        return condition ? expression.Compose(extendExpression, Expression.OrElse) : expression;
    }

    /// <summary>
    /// 根据条件成立再或操作合并两个表达式，支持索引器
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <param name="condition">决定是否应用当前操作的条件</param>
    /// <param name="extendExpression">用于根据条件成立再或操作合并两个表达式，支持索引器的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>根据条件成立再或操作合并两个表达式，支持索引器</returns>
    public static Expression<Func<TSource, int, bool>> OrIf<TSource>(this Expression<Func<TSource, int, bool>> expression,
        bool condition, Expression<Func<TSource, int, bool>> extendExpression)
    {
        return condition ? expression.Compose(extendExpression, Expression.OrElse) : expression;
    }

    /// <summary>
    /// 获取 Lambda 表达式属性名，只限 u=>u.Property 表达式
    /// </summary>
    /// <param name="expression">要组合或执行的表达式</param>
    /// <typeparam name="TSource">源对象类型</typeparam>
    /// <returns>获取到的 Lambda 表达式属性名，只限 u=>u.Property 表达式</returns>
    public static string GetExpressionPropertyName<TSource>(this Expression<Func<TSource, object>> expression)
    {
        if (expression.Body is UnaryExpression unaryExpression)
        {
            return ((MemberExpression)unaryExpression.Operand).Member.Name;
        }

        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is ParameterExpression parameterExpression)
        {
            return parameterExpression.Type.Name;
        }

        throw new InvalidCastException(nameof(expression));
    }
}
