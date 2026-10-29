// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 数据库字段类型映射工具类
/// </summary>
internal partial class SqlSugarDatabaseUtil
{
    /// <summary>
    /// 设置 <see cref="short"/> 类型
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="columnInfo">要补充数据库类型信息的实体列元数据</param>
    internal static void SetDbTypeShort(DbType dbType, EntityColumnInfo columnInfo)
    {
        switch (dbType)
        {
            // MySQL 系列
            case DbType.MySql:
            case DbType.MySqlConnector:
            case DbType.Tidb:
            case DbType.PolarDB:
            case DbType.GBase:
            case DbType.HG:
                columnInfo.DataType = "smallint";
                break;

            // SQL Server
            case DbType.SqlServer:
                columnInfo.DataType = "smallint";
                break;

            // PostgreSQL 系列
            case DbType.PostgreSQL:
            case DbType.OpenGauss:
            case DbType.GaussDB:
            case DbType.GaussDBNative:
            case DbType.TDSQL:
            case DbType.TDSQLForPGODBC:
            case DbType.Vastbase:
            case DbType.Xugu:
            case DbType.GoldenDB:
            case DbType.Doris:
            case DbType.DuckDB:
                columnInfo.DataType = "smallint";
                break;

            // Oracle 系列
            case DbType.Oracle:
            case DbType.OceanBaseForOracle:
                columnInfo.DataType = "number(5,0)";
                break;

            // SQLite
            case DbType.Sqlite:
                columnInfo.DataType = "integer";
                break;

            // ClickHouse
            case DbType.ClickHouse:
                columnInfo.DataType = "int16";
                break;

            // QuestDB
            case DbType.QuestDB:
                columnInfo.DataType = "smallint";
                break;

            // MongoDB
            case DbType.MongoDb:
                columnInfo.DataType = "int32";
                break;

            // Access
            case DbType.Access:
                columnInfo.DataType = "smallint";
                break;

            // 其余兼容分支覆盖 DM、Oscar、Kdbndp、DB2、HANA 和 ODBC
            case DbType.Dm:
            case DbType.Oscar:
            case DbType.Kdbndp:
            case DbType.DB2:
            case DbType.HANA:
            case DbType.Odbc:
                columnInfo.DataType = "smallint";
                break;

            // OceanBase（MySQL 模式）
            case DbType.OceanBase:
                columnInfo.DataType = "smallint";
                break;

            // TDengine 不原生支持 Int32，避免生成该列类型
            case DbType.TDengine:
                throw new SqlSugarException("TDengine 不支持 short 类型，请使用 int 或 float 替代。");

            case DbType.Custom:
            default:
                columnInfo.DataType = "smallint";
                break;
        }
    }
}
