// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.SqlSugar;

/// <summary>
/// 从库连接信息
/// </summary>
/// <remarks>连接字段独立配置，不从主库继承，也不自动执行后配置。</remarks>
[SuppressSniffer]
public class SlaveConnectionInfo
{
    /// <summary>
    /// 服务器 Ip 地址
    /// </summary>
    public string ServiceIp { get; set; }

    /// <summary>
    /// 端口号
    /// </summary>
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
    /// <remarks>自动生成 MySql、MySqlConnector、SqlServer 和 PostgreSQL 连接字符串时使用此值，未指定时使用 100。</remarks>
    public int? MaxPoolSize { get; set; }

    /// <summary>
    /// 超时时间，单位为秒
    /// </summary>
    /// <remarks>当前连接配置生成流程未将此字段应用为独立的从库命令超时。</remarks>
    public int? CommandTimeOut { get; set; }

    /// <summary>
    /// 从库命中率
    /// </summary>
    /// <remarks>
    /// <para>为 0 则不命中</para>
    /// <para>建议相加不超过 10</para>
    /// </remarks>
    public int HitRate { get; set; }
}
