// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlSugar;
using Yitter.IdGenerator;

namespace Fast.SqlSugar;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供动态 API 扩展方法
/// </summary>
[SuppressSniffer]
public static class IServiceCollectionExtension
{
    /// <summary>
    /// 添加雪花Id
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="section">JSON 配置文件节点的 Key 默认值：SnowflakeSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddSnowflake(this IServiceCollection services, IConfiguration configuration,
        string section = "SnowflakeSettings")
    {
        Debugging.Info("Registering snowflake......");

        services.AddConfigurableOptions<SnowflakeSettingsOptions>(section);

        SqlSugarContext.SnowflakeSettings = configuration.GetSection(section).Get<SnowflakeSettingsOptions>().LoadPostConfigure();

        // 每个实例必须使用不同的雪花算法 WorkerId，避免生成重复Id
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions {WorkerId = SqlSugarContext.SnowflakeSettings.WorkerId ?? 1});

        return services;
    }

    /// <summary>
    /// 添加雪花Id
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="optionAction">雪花Id配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddSnowflake(this IServiceCollection services, IConfiguration configuration,
        Action<SnowflakeSettingsOptions> optionAction)
    {
        Debugging.Info("Registering snowflake......");

        services.Configure(optionAction);

        var snowflakeSettings = new SnowflakeSettingsOptions();
        optionAction.Invoke(snowflakeSettings);

        // 每个实例必须使用不同的雪花算法 WorkerId，避免生成重复Id
        YitIdHelper.SetIdGenerator(new IdGeneratorOptions {WorkerId = SqlSugarContext.SnowflakeSettings.WorkerId ?? 1});

        return services;
    }

    /// <summary>
    /// 注册 SqlSugar 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="hostEnvironment">当前应用的宿主环境</param>
    /// <param name="connectionSection">JSON 配置文件节点的 Key 默认值：ConnectionSettings</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment hostEnvironment, string connectionSection = "ConnectionSettings")
    {
        Debugging.Info("Registering sql sugar......");

        services.AddConfigurableOptions<ConnectionSettingsOptions>(connectionSection);

        ConnectionSettingsOptions connectionSettings = configuration
            .GetSection(connectionSection)
            .Get<ConnectionSettingsOptions>();

        SqlSugarContext.ConnectionSettings = connectionSettings;

        services.AddSqlSugar(hostEnvironment);

        return services;
    }

    /// <summary>
    /// 注册 SqlSugar 服务
    /// </summary>
    /// <param name="services">要添加服务的服务集合</param>
    /// <param name="configuration">用于读取模块设置的配置</param>
    /// <param name="hostEnvironment">当前应用的宿主环境</param>
    /// <param name="optionAction">数据库连接配置操作</param>
    /// <returns>返回 <paramref name="services"/>，便于链式调用</returns>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, IConfiguration configuration,
        IHostEnvironment hostEnvironment, Action<ConnectionSettingsOptions> optionAction)
    {
        Debugging.Info("Registering sql sugar......");

        services.Configure(optionAction);

        var connectionSettings = new ConnectionSettingsOptions();
        optionAction.Invoke(connectionSettings);

        SqlSugarContext.ConnectionSettings = connectionSettings;

        services.AddSqlSugar(hostEnvironment);

        return services;
    }

    /// <summary>
    /// 注册 SqlSugar 服务
    /// </summary>
    /// <param name="services">服务注册集合</param>
    /// <param name="hostEnvironment">当前应用的宿主环境</param>
    private static void AddSqlSugar(this IServiceCollection services, IHostEnvironment hostEnvironment)
    {
        // 查找 Sugar 实体处理程序提供者
        Type iSqlSugarEntityHandlerType =
            MAppContext.EffectiveTypes.FirstOrDefault(f => typeof(ISqlSugarEntityHandler).IsAssignableFrom(f) && !f.IsInterface);
        if (iSqlSugarEntityHandlerType != null)
        {
            // 注册 Sugar 实体处理程序
            services.AddScoped(typeof(ISqlSugarEntityHandler), iSqlSugarEntityHandlerType);
        }

        // 同时注册具体类型，支持调用方不经 ISqlSugarClient 接口直接解析客户端
        services.AddScoped<ISqlSugarClient>(serviceProvider =>
        {
            // 获取 Sugar 实体处理 接口的实现类
            ISqlSugarEntityHandler sqlSugarEntityHandler = serviceProvider.GetRequiredService<ISqlSugarEntityHandler>();

            var sqlSugarClient = new SqlSugarClient(SqlSugarContext.GetConnectionConfig(SqlSugarContext.ConnectionSettings));

            sqlSugarClient.Ado.CommandTimeOut = SqlSugarContext.ConnectionSettings.CommandTimeOut!.Value;

            SugarEntityFilter.LoadSugarAop(hostEnvironment.IsDevelopment(), sqlSugarClient,
                SqlSugarContext.ConnectionSettings.SugarSqlExecMaxSeconds!.Value,
                SqlSugarContext.ConnectionSettings.DiffLog!.Value, SqlSugarContext.ConnectionSettings.DisableAop!.Value,
                sqlSugarEntityHandler);

            SugarEntityFilter.LoadSugarFilter(sqlSugarClient, sqlSugarEntityHandler);

            return sqlSugarClient;
        });

        // 注册泛型仓储
        services.AddScoped(typeof(ISqlSugarRepository<>), typeof(SqlSugarRepository<>));
    }
}
