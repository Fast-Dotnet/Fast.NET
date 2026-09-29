// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Linq.Expressions;
using System.Reflection;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 删除仓储实现
/// </summary>
internal sealed partial class SqlSugarRepository<TEntity>
{
    /// <inheritdoc />
    public int Delete(TEntity entity)
    {
        IDeleteable<TEntity> deleteable = Deleteable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommand();
        }

        return deleteable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(TEntity entity)
    {
        IDeleteable<TEntity> deleteable = Deleteable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommandAsync();
        }

        return deleteable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Delete(object key)
    {
        IDeleteable<TEntity> deleteable = Deleteable<TEntity>().In(key).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommand();
        }

        return deleteable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(object key)
    {
        IDeleteable<TEntity> deleteable = Deleteable<TEntity>().In(key).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommandAsync();
        }

        return deleteable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Delete(params object[] keys)
    {
        IDeleteable<TEntity> deleteable = Deleteable<TEntity>().In(keys).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommand();
        }

        return deleteable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(params object[] keys)
    {
        IDeleteable<TEntity> deleteable = Deleteable<TEntity>().In(keys).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommandAsync();
        }

        return deleteable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Delete(params TEntity[] entities)
    {
        IDeleteable<TEntity> deleteable = Deleteable(entities.ToList()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommand();
        }

        return deleteable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(params TEntity[] entities)
    {
        IDeleteable<TEntity> deleteable = Deleteable(entities.ToList()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommandAsync();
        }

        return deleteable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Delete(IEnumerable<TEntity> entities)
    {
        IDeleteable<TEntity> deleteable = Deleteable(entities.ToList()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommand();
        }

        return deleteable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(IEnumerable<TEntity> entities)
    {
        IDeleteable<TEntity> deleteable = Deleteable(entities.ToList()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommandAsync();
        }

        return deleteable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Delete(Expression<Func<TEntity, bool>> whereExpression)
    {
        IDeleteable<TEntity> deleteable = Deleteable<TEntity>()
            .Where(whereExpression)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommand();
        }

        return deleteable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> DeleteAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        IDeleteable<TEntity> deleteable = Deleteable<TEntity>()
            .Where(whereExpression)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return deleteable.SplitTable().ExecuteCommandAsync();
        }

        return deleteable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int LogicDelete(Expression<Func<TEntity, bool>> whereExpression)
    {
        if (!SupportsLogicDelete)
        {
            throw new InvalidOperationException(
                $"{nameof(TEntity)} does not inherit {nameof(IDeletedEntity)} interface, Logical deletion cannot be used.");
        }

        TEntity deletedEntity = Activator.CreateInstance<TEntity>();

        PropertyInfo isDeletedProperty = typeof(TEntity).GetProperty(nameof(IDeletedEntity.IsDeleted));

        isDeletedProperty!.SetValue(deletedEntity, true);

        IUpdateable<TEntity> updateable = Updateable<TEntity>()
            .Where(whereExpression)
            .SetColumns(_ => deletedEntity, true)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommand();
        }

        return updateable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> LogicDeleteAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        if (!SupportsLogicDelete)
        {
            throw new InvalidOperationException(
                $"{nameof(TEntity)} does not inherit {nameof(IDeletedEntity)} interface, Logical deletion cannot be used.");
        }

        TEntity deletedEntity = Activator.CreateInstance<TEntity>();

        PropertyInfo isDeletedProperty = typeof(TEntity).GetProperty(nameof(IDeletedEntity.IsDeleted));

        isDeletedProperty!.SetValue(deletedEntity, true);

        IUpdateable<TEntity> updateable = Updateable<TEntity>()
            .Where(whereExpression)
            .SetColumns(_ => deletedEntity, true)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommandAsync();
        }

        return updateable.ExecuteCommandAsync();
    }
}
