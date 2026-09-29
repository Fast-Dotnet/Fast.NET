// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 插入仓储实现
/// </summary>
internal sealed partial class SqlSugarRepository<TEntity>
{
    /// <inheritdoc />
    public int Insert(TEntity entity)
    {
        IInsertable<TEntity> insertable = Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return insertable.SplitTable().ExecuteCommand();
        }

        return insertable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> InsertAsync(TEntity entity)
    {
        IInsertable<TEntity> insertable = Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return insertable.SplitTable().ExecuteCommandAsync();
        }

        return insertable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Insert(params TEntity[] entities)
    {
        IInsertable<TEntity> insertable = Insertable(entities).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return insertable.SplitTable().ExecuteCommand();
        }

        return insertable.ExecuteCommand();
    }

    /// <inheritdoc />
    public Task<int> InsertAsync(params TEntity[] entities)
    {
        IInsertable<TEntity> insertable = Insertable(entities).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

        if (IsSplitTable)
        {
            return insertable.SplitTable().ExecuteCommandAsync();
        }

        return insertable.ExecuteCommandAsync();
    }

    /// <inheritdoc />
    public int Insert(IEnumerable<TEntity> entities)
    {
        TEntity[] _entities = entities?.ToArray();
        if (_entities?.Length > 0)
        {
            IInsertable<TEntity> insertable = Insertable(_entities.ToArray()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

            if (IsSplitTable)
            {
                return insertable.SplitTable().ExecuteCommand();
            }

            return insertable.ExecuteCommand();
        }

        return 0;
    }

    /// <inheritdoc />
    public Task<int> InsertAsync(IEnumerable<TEntity> entities)
    {
        TEntity[] _entities = entities?.ToArray();
        if (_entities?.Length > 0)
        {
            IInsertable<TEntity> insertable = Insertable(_entities.ToArray()).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value);

            if (IsSplitTable)
            {
                return insertable.SplitTable().ExecuteCommandAsync();
            }

            return insertable.ExecuteCommandAsync();
        }

        return Task.FromResult(0);
    }

    /// <inheritdoc />
    public int InsertReturnIdentity(TEntity entity)
    {
        return Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value).ExecuteReturnIdentity();
    }

    /// <inheritdoc />
    public Task<int> InsertReturnIdentityAsync(TEntity entity)
    {
        return Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value).ExecuteReturnIdentityAsync();
    }

    /// <inheritdoc />
    public long ExecuteReturnBigIdentity(TEntity entity)
    {
        return Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value).ExecuteReturnBigIdentity();
    }

    /// <inheritdoc />
    public Task<long> ExecuteReturnBigIdentityAsync(TEntity entity)
    {
        return Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value).ExecuteReturnBigIdentityAsync();
    }

    /// <inheritdoc />
    public TEntity InsertReturnEntity(TEntity entity)
    {
        return Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value).ExecuteReturnEntity();
    }

    /// <inheritdoc />
    public Task<TEntity> InsertReturnEntityAsync(TEntity entity)
    {
        return Insertable(entity).EnableDiffLogEventIF(DatabaseInfo.DiffLog!.Value).ExecuteReturnEntityAsync();
    }
}
