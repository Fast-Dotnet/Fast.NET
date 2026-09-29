// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 仓储实现
/// </summary>
internal sealed partial class SqlSugarRepository<TEntity> : SqlSugarClient, ISqlSugarRepository<TEntity>
    where TEntity : class, new()
{
    /// <summary>
    /// 服务提供器
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// SqlSugar 仓储实现
    /// </summary>
    /// <param name="hostEnvironment">当前应用的宿主环境</param>
    /// <param name="serviceProvider">用于解析服务的服务提供器；为 <see langword="null"/> 时使用当前请求或根服务提供器</param>
    public SqlSugarRepository(IHostEnvironment hostEnvironment, IServiceProvider serviceProvider) : base(
        SqlSugarContext.GetConnectionConfig(SqlSugarContext.ConnectionSettings))
    {
        _serviceProvider = serviceProvider;

        Type entityType = typeof(TEntity);

        // 是否支持逻辑删除
        SupportsLogicDelete = typeof(IDeletedEntity).IsAssignableFrom(entityType);
        // 是否支持行版本控制
        SupportsRowVersion = typeof(IUpdateVersion).IsAssignableFrom(entityType);

        // 是否分表，判断是否存在 SplitTableAttribute 特性
        IsSplitTable = entityType.GetCustomAttribute<SplitTableAttribute>(true) != null;

        // 获取当前实体类头部的 SugarDbTypeAttribute 特性
        SugarDbTypeAttribute sugarDbTypeAttribute = entityType.GetCustomAttribute<SugarDbTypeAttribute>(true);

        // 根据 TEntity 加载对应的数据库连接字符串
        ISqlSugarEntityHandler sqlSugarEntityHandler = _serviceProvider.GetService<ISqlSugarEntityHandler>();

        // 获取新的连接字符串
        // DI 构造函数不能声明为 async；这里显式解包异步结果，避免 .Result 将真实异常包装成 AggregateException
        ConnectionSettingsOptions connectionSettings = sqlSugarEntityHandler
            ?.GetConnectionSettings<TEntity>(Context, sugarDbTypeAttribute, entityType)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        // 数据库信息
        DatabaseInfo = connectionSettings ?? SqlSugarContext.ConnectionSettings;

        // 重新初始化 Context
        InitContext(SqlSugarContext.GetConnectionConfig(DatabaseInfo));

        Context.Ado.CommandTimeOut = DatabaseInfo.CommandTimeOut!.Value;

        SugarEntityFilter.LoadSugarAop(hostEnvironment.IsDevelopment(), Context, DatabaseInfo.SugarSqlExecMaxSeconds!.Value,
            DatabaseInfo.DiffLog!.Value, DatabaseInfo.DisableAop!.Value, sqlSugarEntityHandler);

        SugarEntityFilter.LoadSugarFilter(Context, sqlSugarEntityHandler);
    }

    /// <inheritdoc />
    public bool SupportsLogicDelete { get; }

    /// <inheritdoc />
    public bool SupportsRowVersion { get; }

    /// <inheritdoc />
    public bool IsSplitTable { get; }

    /// <summary>
    /// 实体集合
    /// </summary>
    public ISugarQueryable<TEntity> Entities => Queryable<TEntity>();

    /// <inheritdoc />
    public ConnectionSettingsOptions DatabaseInfo { get; set; }

    /// <inheritdoc />
    public ISqlSugarRepository<TChangeEntity> Change<TChangeEntity>() where TChangeEntity : class, new()
    {
        return _serviceProvider.GetService(typeof(ISqlSugarRepository<TChangeEntity>)) as ISqlSugarRepository<TChangeEntity>;
    }
}
