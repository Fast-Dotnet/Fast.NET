// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 上下文
/// </summary>
[SuppressSniffer]
public sealed class SqlSugarContext
{
    /// <summary>
    /// 连接字符串配置
    /// </summary>
    public static ConnectionSettingsOptions ConnectionSettings { get; internal set; }

    /// <summary>
    /// 雪花Id配置
    /// </summary>
    public static SnowflakeSettingsOptions SnowflakeSettings { get; internal set; }

    /// <summary>
    /// 最大不分页大小限制
    /// </summary>
    public static int MaxNotPageSize { get; set; }

    /// <summary>
    /// SqlSugar 实体集合
    /// </summary>
    public static readonly List<SqlSugarEntityInfo> SqlSugarEntityList;

    static SqlSugarContext()
    {
        MaxNotPageSize = 10000;

        Type dataBaseEntityType = typeof(IDatabaseEntity);

        SqlSugarEntityList = MAppContext.EffectiveTypes.Where(wh => dataBaseEntityType.IsAssignableFrom(wh) && !wh.IsInterface)
            .Select(sl =>
            {
                SugarTable sqlSugarTableAttribute = sl.GetSugarTableAttribute();

                SplitTableAttribute splitTableAttribute = sl.GetCustomAttribute<SplitTableAttribute>(true);

                SugarDbTypeAttribute sugarDbTypeAttribute = sl.GetCustomAttribute<SugarDbTypeAttribute>(true);

                return new SqlSugarEntityInfo
                {
                    TableName = sqlSugarTableAttribute?.TableName ?? sl.Name,
                    TableDescription = sqlSugarTableAttribute?.TableDescription,
                    EntityType = sl,
                    IsSplitTable = splitTableAttribute != null,
                    SugarDbType = sugarDbTypeAttribute?.Type
                };
            })
            .ToList();
    }

    /// <summary>
    /// 获取连接配置
    /// </summary>
    /// <param name="connectionSettings">connection Settings 配置</param>
    /// <returns>获取到的连接配置</returns>
    public static ConnectionConfig GetConnectionConfig(ConnectionSettingsOptions connectionSettings)
    {
        // 得到连接字符串
        string connectionStr = SqlSugarDatabaseUtil.GetConnectionStr(connectionSettings.DbType!.Value, connectionSettings);

        var slaveConnectionList = new List<SlaveConnectionConfig>();

        // 判断是否存在从库信息
        if (connectionSettings.SlaveConnectionList is {Count: > 0})
        {
            foreach (SlaveConnectionInfo slaveConnectionInfo in connectionSettings.SlaveConnectionList)
            {
                string slaveConnectionStr =
                    SqlSugarDatabaseUtil.GetConnectionStr(connectionSettings.DbType!.Value, slaveConnectionInfo);

                slaveConnectionList.Add(new SlaveConnectionConfig
                {
                    HitRate = slaveConnectionInfo.HitRate, ConnectionString = slaveConnectionStr
                });
            }
        }

        return new ConnectionConfig
        {
            // 此链接标志，用以后面切库使用
            ConfigId = connectionSettings.ConnectionId,
            ConnectionString = connectionStr,
            DbType = connectionSettings.DbType!.Value,
            // 每次操作完成后释放连接，避免作用域仓储长期占用连接
            IsAutoCloseConnection = true,
            // 从特性读取主键和自增列信息
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = SqlSugarDatabaseUtil.GetSugarMoreSettings(),
            ConfigureExternalServices = SqlSugarDatabaseUtil.GetSugarExternalServices(connectionSettings.DbType!.Value),
            SlaveConnectionConfigs = slaveConnectionList
        };
    }

    /// <summary>
    /// Entity Value 检测
    /// </summary>
    /// <param name="emptyList">空对象检测集合</param>
    /// <param name="entityInfo">实体信息</param>
    /// <returns>检查通过时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    internal static bool EntityValueCheck(ICollection<object> emptyList, DataFilterModel entityInfo)
    {
        try
        {
            object propertyValue = entityInfo.EntityColumnInfo.PropertyInfo.GetValue(entityInfo.EntityValue);
            return emptyList == null || emptyList.Any(empty => Equals(empty, propertyValue));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 设置 Entity Value
    /// </summary>
    /// <param name="fieldName">字段名称</param>
    /// <param name="emptyList">空对象检测集合</param>
    /// <param name="setValue">要设置的值</param>
    /// <param name="entityInfo">实体信息</param>
    internal static void SetEntityValue(string fieldName, ICollection<object> emptyList, object setValue,
        DataFilterModel entityInfo)
    {
        // 判断属性名称是否等于传入的字段名称
        if (entityInfo.PropertyName == fieldName)
        {
            if (EntityValueCheck(emptyList, entityInfo))
            {
                entityInfo.SetValue(setValue);
            }
        }
    }
}
