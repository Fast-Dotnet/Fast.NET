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
/// SqlSugar 更新仓储实现
/// </summary>
internal sealed partial class SqlSugarRepository<TEntity>
{
    /// <inheritdoc />
    public int Update(TEntity entity, bool isNoUpdateNull = false)
    {
        IUpdateable<TEntity> updateable = Updateable(entity)
            .IgnoreColumns(isNoUpdateNull)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return SupportsRowVersion
                ? updateable.SplitTable().ExecuteCommandWithOptLock(true)
                : updateable.SplitTable().ExecuteCommand();
        }

        return SupportsRowVersion ? updateable.ExecuteCommandWithOptLock(true) : updateable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> UpdateAsync(TEntity entity, bool isNoUpdateNull = false)
    {
        IUpdateable<TEntity> updateable = Updateable(entity)
            .IgnoreColumns(isNoUpdateNull)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return SupportsRowVersion
                ? updateable.SplitTable().ExecuteCommandWithOptLockAsync(true)
                : updateable.SplitTable().ExecuteCommandAsync();
        }

        return SupportsRowVersion ? updateable.ExecuteCommandWithOptLockAsync(true) : updateable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Update(params TEntity[] entities)
    {
        IUpdateable<TEntity> updateable = Updateable(entities).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommand();
        }

        return updateable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> UpdateAsync(params TEntity[] entities)
    {
        IUpdateable<TEntity> updateable = Updateable(entities).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommandAsync();
        }

        return updateable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Update(IEnumerable<TEntity> entities)
    {
        IUpdateable<TEntity> updateable = Updateable(entities.ToArray()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommand();
        }

        return updateable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> UpdateAsync(IEnumerable<TEntity> entities)
    {
        IUpdateable<TEntity> updateable = Updateable(entities.ToArray()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommandAsync();
        }

        return updateable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int UpdateNoPrimaryKey(TEntity entity, Expression<Func<TEntity, object>> columns)
    {
        IUpdateable<TEntity> updateable = Updateable(entity)
            .WhereColumns(columns)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommand();
        }

        return updateable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> UpdateNoPrimaryKeyAsync(TEntity entity, Expression<Func<TEntity, object>> columns)
    {
        IUpdateable<TEntity> updateable = Updateable(entity)
            .WhereColumns(columns)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommandAsync();
        }

        return updateable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int UpdateNoPrimaryKey(List<TEntity> entity, Expression<Func<TEntity, object>> columns)
    {
        IUpdateable<TEntity> updateable = Updateable(entity)
            .WhereColumns(columns)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommand();
        }

        return updateable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> UpdateNoPrimaryKeyAsync(List<TEntity> entity, Expression<Func<TEntity, object>> columns)
    {
        IUpdateable<TEntity> updateable = Updateable(entity)
            .WhereColumns(columns)
            .EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return updateable.SplitTable().ExecuteCommandAsync();
        }

        return updateable.ExecuteCommandAsync();
    }
}
