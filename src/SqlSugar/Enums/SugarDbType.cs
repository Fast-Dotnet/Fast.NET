// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel;

namespace Fast.SqlSugar;

/// <summary>
/// Sugar 数据库类型枚举
/// </summary>
[FastEnum("Sugar数据库类型枚举")]
public enum SugarDbType
{
    /// <summary>
    /// MySql
    /// </summary>
    [Description("MySql")]
    MySql = 1,

    /// <summary>
    /// SqlServer
    /// </summary>
    [Description("SqlServer")]
    SqlServer = 2,

    /// <summary>
    /// SqLite
    /// </summary>
    [Description("SqLite")]
    Sqlite = 3,

    /// <summary>
    /// Oracle
    /// </summary>
    [Description("Oracle")]
    Oracle = 4,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    [Description("PostgreSQL")]
    PostgreSQL = 5,

    /// <summary>
    /// MongoDB
    /// </summary>
    [Description("MongoDB")]
    MongoDb = 6
}
