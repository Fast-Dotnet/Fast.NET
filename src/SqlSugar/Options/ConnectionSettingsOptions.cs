// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 连接字符串配置
/// </summary>
[SuppressSniffer]
public class ConnectionSettingsOptions : IPostConfigure
{
    /// <summary>
    /// SqlSugarClient 连接Id
    /// </summary>
    public string ConnectionId { get; set; }

    /// <summary>
    /// 数据库类型，用于区分使用的是那个类型的数据库
    /// </summary>
    public DbType? DbType { get; set; }

    /// <summary>
    /// 服务器 Ip 地址
    /// </summary>
    public string ServiceIp { get; set; }

    /// <summary>
    /// 端口号
    /// </summary>
    /// <remarks>
    /// <para>MySql：3306</para>
    /// <para>SqlServer：1433</para>
    /// <para>Oracle：1521</para>
    /// <para>PostgreSQL：5432</para>
    /// <para>MongoDb：27017</para>
    /// <para><see cref="PostConfigure"/> 将未指定的端口设为 1433，不会根据数据库类型自动选择端口。</para>
    /// </remarks>
    public int? Port { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DbName { get; set; }

    /// <summary>
    /// 数据库用户
    /// </summary>
    public string DbUser { get; set; }

    /// <summary>
    /// 数据库密码
    /// </summary>
    public string DbPwd { get; set; }

    /// <summary>
    /// 最大连接池数量
    /// </summary>
    /// <remarks>
    /// <see cref="PostConfigure"/> 将未指定的值设为 100。
    /// 自动生成 MySql、MySqlConnector、SqlServer 和 PostgreSQL 连接字符串时使用此值。
    /// 使用 <see cref="CustomConnectionStr"/> 时，应在自定义连接字符串中配置连接池。
    /// </remarks>
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// 超时时间，单位为秒
    /// </summary>
    public int? CommandTimeOut { get; set; }

    /// <summary>
    /// SqlSugar SQL 执行最大秒数，如果超过记录警告日志
    /// </summary>
    public int? SugarSqlExecMaxSeconds { get; set; }

    /// <summary>
    /// 差异日志
    /// </summary>
    public bool? DiffLog { get; set; }

    /// <summary>
    /// 禁用 SqlSugar 的 AOP
    /// </summary>
    /// <remarks>
    /// <para>如果是通过 <see cref="ISqlSugarEntityHandler"/> 进行保存日志到数据库中</para>
    /// <para>必须要将相关 AOP 中涉及到的日志表，单独进行分库设置，并且禁用 AOP</para>
    /// <para>或通过 new <see cref="SqlSugarClient"/>() 的方式进行保存。不然会存在死循环的问题</para>
    /// </remarks>
    public bool? DisableAop { get; set; }

    /// <summary>
    /// 自定义连接字符串
    /// </summary>
    /// <remarks>
    /// 非空白时直接使用此连接字符串，并忽略 <see cref="SlaveConnectionList"/>。
    /// 服务器、端口、数据库、凭据及连接池字段不会再用于拼接主库连接字符串。
    /// </remarks>
    public virtual string CustomConnectionStr { get; set; }

    /// <summary>
    /// 从库信息
    /// </summary>
    /// <remarks>一般默认库或者主库不建议设置从库</remarks>
    public List<SlaveConnectionInfo> SlaveConnectionList { get; set; }

    /// <inheritdoc />
    public void PostConfigure()
    {
        DbType ??= global::SqlSugar.DbType.SqlServer;
        ServiceIp ??= "127.0.0.1";
        Port ??= 1433;
        DbUser ??= "sa";
        MaxPoolSize ??= 100;
        CommandTimeOut ??= 60;
        SugarSqlExecMaxSeconds ??= 30;
        DiffLog ??= false;
        DisableAop ??= true;
    }
}
