// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Linq.Expressions;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 查询仓储接口
/// </summary>
public partial interface ISqlSugarRepository<TEntity>
{
    /// <summary>
    /// 获取总数
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>满足条件的项数</returns>
    int Count(Expression<Func<TEntity, bool>> whereExpression = null);

    /// <summary>
    /// 获取总数
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>表示异步获取总数的任务，任务结果为满足条件的项数</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression = null);

    /// <summary>
    /// 检查是否存在
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Any(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 检查是否存在
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="Id">唯一标识</param>
    /// <returns>根据主键获取实体</returns>
    TEntity SingleOrDefault(object Id);

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="Id">唯一标识</param>
    /// <returns>表示异步根据主键获取实体的任务，任务结果为根据主键获取实体</returns>
    Task<TEntity> SingleOrDefaultAsync(object Id);

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>根据条件获取实体</returns>
    TEntity SingleOrDefault(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>表示异步根据条件获取实体的任务，任务结果为根据条件获取实体</returns>
    Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 获取一个实体
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>获取到的一个实体</returns>
    TEntity FirstOrDefault(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 获取一个实体
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>表示异步获取一个实体的任务，任务结果为获取到的一个实体</returns>
    Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <returns>获取到的列表集合</returns>
    List<TEntity> ToList();

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <returns>表示异步获取列表的任务，任务结果为获取到的列表集合</returns>
    Task<List<TEntity>> ToListAsync();

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>获取到的列表集合</returns>
    List<TEntity> ToList(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <returns>表示异步获取列表的任务，任务结果为获取到的列表集合</returns>
    Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> whereExpression);

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <param name="orderByExpression">用于指定排序字段的表达式</param>
    /// <param name="orderByType">排序方向</param>
    /// <returns>获取到的列表集合</returns>
    List<TEntity> ToList(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, object>> orderByExpression,
        OrderByType orderByType = OrderByType.Asc);

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <param name="whereExpression">用于筛选实体的条件表达式</param>
    /// <param name="orderByExpression">用于指定排序字段的表达式</param>
    /// <param name="orderByType">排序方向</param>
    /// <returns>表示异步获取列表的任务，任务结果为获取到的列表集合</returns>
    Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> whereExpression,
        Expression<Func<TEntity, object>> orderByExpression, OrderByType orderByType = OrderByType.Asc);
}
