// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 仓储接口
/// </summary>
public partial interface ISqlSugarRepository<TEntity> : ISqlSugarClient where TEntity : class, new()
{
    /// <summary>
    /// 是否支持逻辑删除
    /// </summary>
    /// <remarks><typeparamref name="TEntity"/> 继承了 <see cref="IDeletedEntity"/> 才有用</remarks>
    bool SupportsLogicDelete { get; }

    /// <summary>
    /// 是否支持行版本控制（乐观锁）
    /// </summary>
    /// <remarks><typeparamref name="TEntity"/> 继承了 <see cref="IUpdateVersion"/> 才有用</remarks>
    bool SupportsRowVersion { get; }

    /// <summary>
    /// 是否分表
    /// </summary>
    /// <remarks><typeparamref name="TEntity"/> 头部标记 <see cref="SplitTableAttribute"/> 特性才有用</remarks>
    bool IsSplitTable { get; }

    /// <summary>
    /// 当前仓储的数据库信息
    /// </summary>
    ConnectionSettingsOptions DatabaseInfo { get; }

    /// <summary>
    /// 切换仓储/切换租户仓储
    /// </summary>
    /// <typeparam name="TChangeEntity">临时切换使用的实体类型</typeparam>
    /// <returns>切换仓储/切换租户仓储</returns>
    ISqlSugarRepository<TChangeEntity> Change<TChangeEntity>() where TChangeEntity : class, new();
}
