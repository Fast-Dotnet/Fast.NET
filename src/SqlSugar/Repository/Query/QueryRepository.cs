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
/// SqlSugar 查询仓储实现
/// </summary>
internal sealed partial class SqlSugarRepository<TEntity>
{
    /// <inheritdoc />
    public int Count(Expression<Func<TEntity, bool>> whereExpression = null)
    {
        return Queryable<TEntity>().WhereIF(whereExpression != null, whereExpression).Count();
    }

    /// <inheritdoc />
    public Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression = null)
    {
        return Queryable<TEntity>().WhereIF(whereExpression != null, whereExpression).CountAsync();
    }

    /// <inheritdoc />
    public bool Any(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Queryable<TEntity>().Any(whereExpression);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return await Queryable<TEntity>().AnyAsync(whereExpression);
    }

    /// <inheritdoc />
    public TEntity SingleOrDefault(object Id)
    {
        return Queryable<TEntity>().InSingle(Id);
    }

    /// <inheritdoc />
    public Task<TEntity> SingleOrDefaultAsync(object Id)
    {
        return Queryable<TEntity>().InSingleAsync(Id);
    }

    /// <inheritdoc />
    public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Queryable<TEntity>().Single(whereExpression);
    }

    /// <inheritdoc />
    public Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Queryable<TEntity>().SingleAsync(whereExpression);
    }

    /// <inheritdoc />
    public TEntity FirstOrDefault(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Queryable<TEntity>().First(whereExpression);
    }

    /// <inheritdoc />
    public async Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return await Queryable<TEntity>().FirstAsync(whereExpression);
    }

    /// <inheritdoc />
    public List<TEntity> ToList()
    {
        return Queryable<TEntity>().ToList();
    }

    /// <inheritdoc />
    public Task<List<TEntity>> ToListAsync()
    {
        return Queryable<TEntity>().ToListAsync();
    }

    /// <inheritdoc />
    public List<TEntity> ToList(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Queryable<TEntity>().Where(whereExpression).ToList();
    }

    /// <inheritdoc />
    public Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        return Queryable<TEntity>().Where(whereExpression).ToListAsync();
    }

    /// <inheritdoc />
    public List<TEntity> ToList(Expression<Func<TEntity, bool>> whereExpression,
        Expression<Func<TEntity, object>> orderByExpression, OrderByType orderByType = OrderByType.Asc)
    {
        return Queryable<TEntity>().Where(whereExpression).OrderBy(orderByExpression, orderByType).ToList();
    }

    /// <inheritdoc />
    public Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> whereExpression,
        Expression<Func<TEntity, object>> orderByExpression, OrderByType orderByType = OrderByType.Asc)
    {
        return Queryable<TEntity>().Where(whereExpression).OrderBy(orderByExpression, orderByType).ToListAsync();
    }
}
