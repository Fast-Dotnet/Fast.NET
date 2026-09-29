// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="EnumExtension"/> 提供 GroupBy 扩展方法
/// </summary>
public static class GroupByExtension
{
    /// <summary>
    /// 多个 GroupBy
    /// </summary>
    /// <param name="source">源对象</param>
    /// <param name="groupByProperties">参与分组的属性名称集合</param>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <returns>多个 GroupBy 集合</returns>
    public static IEnumerable<IGrouping<string, TKey>> GroupByMultiple<TKey>(this IEnumerable<TKey> source,
        params Expression<Func<TKey, object>>[] groupByProperties)
    {
        IQueryable<TKey> query = source.AsQueryable();
        ParameterExpression parameter = Expression.Parameter(typeof(TKey), "gb");
        Expression keySelector = null;

        foreach (Expression<Func<TKey, object>> property in groupByProperties)
        {
            InvocationExpression memberExpression = Expression.Invoke(property, parameter);
            UnaryExpression conversionExpression = Expression.Convert(memberExpression, typeof(object));
            ConditionalExpression nullCheckExpression = Expression.Condition(
                Expression.Equal(memberExpression, Expression.Constant(null)), Expression.Constant(""),
                Expression.Call(conversionExpression, "ToString", null));

            if (keySelector == null)
            {
                keySelector = nullCheckExpression;
            }
            else
            {
                keySelector = Expression.Call(typeof(string).GetMethod("Concat", new[] {typeof(string), typeof(string)}),
                    keySelector, nullCheckExpression);
            }
        }

        var lambda = Expression.Lambda<Func<TKey, string>>(keySelector, parameter);
        MethodCallExpression groupByExpression = Expression.Call(typeof(Queryable), "GroupBy",
            new[] {typeof(TKey), typeof(string)}, query.Expression, lambda);
        IQueryable<IGrouping<string, TKey>> result = query.Provider.CreateQuery<IGrouping<string, TKey>>(groupByExpression);

        return result;
    }
}
