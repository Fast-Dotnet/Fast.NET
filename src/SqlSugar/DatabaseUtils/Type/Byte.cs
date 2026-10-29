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
    /// 设置 <see cref="byte"/> 类型
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="columnInfo">要补充数据库类型信息的实体列元数据</param>
    internal static void SetDbTypeByte(DbType dbType, EntityColumnInfo columnInfo)
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
                columnInfo.DataType = "tinyint unsigned";
                break;

            // SQL Server
            case DbType.SqlServer:
                columnInfo.DataType = "tinyint";
                break;

            // PostgreSQL 系列
            case DbType.PostgreSQL:
            case DbType.OpenGauss:
            case DbType.TDSQLForPGODBC:
            case DbType.TDSQL:
            case DbType.GaussDB:
            case DbType.GaussDBNative:
            case DbType.Vastbase:
            case DbType.Xugu:
            case DbType.Doris:
            case DbType.GoldenDB:
            case DbType.DuckDB:
                columnInfo.DataType = "smallint";
                break;

            // Oracle 系列
            case DbType.Oracle:
            case DbType.OceanBaseForOracle:
                columnInfo.DataType = "number(3,0)";
                break;

            // SQLite
            case DbType.Sqlite:
                columnInfo.DataType = "integer";
                break;

            // ClickHouse
            case DbType.ClickHouse:
                columnInfo.DataType = "uint8";
                break;

            // Access
            case DbType.Access:
                columnInfo.DataType = "byte";
                break;

            // MongoDB
            case DbType.MongoDb:
                columnInfo.DataType = "int32";
                break;

            // 其他类关系型数据库统一 smallint
            case DbType.Dm:
            case DbType.Oscar:
            case DbType.Kdbndp:
            case DbType.DB2:
            case DbType.HANA:
            case DbType.Odbc:
                columnInfo.DataType = "smallint";
                break;

            // QuestDB：建议用 short 避免溢出
            case DbType.QuestDB:
                columnInfo.DataType = "smallint";
                break;

            // TDengine 支持 tinyint，不支持 byte 类型的特殊含义
            case DbType.TDengine:
                columnInfo.DataType = "tinyint";
                break;

            case DbType.OceanBase:
            case DbType.Custom:
            default:
                columnInfo.DataType = "tinyint";
                break;
        }
    }
}
