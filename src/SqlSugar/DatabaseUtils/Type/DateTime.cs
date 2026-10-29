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
    /// 设置 <see cref="DateTime"/> 类型
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="columnInfo">要补充数据库类型信息的实体列元数据</param>
    internal static void SetDbTypeDateTime(DbType dbType, EntityColumnInfo columnInfo)
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
            case DbType.Oscar:
            case DbType.Odbc:
            case DbType.Access:
            case DbType.MongoDb:
            case DbType.ClickHouse:
                columnInfo.DataType = "datetime";
                break;

            // SQL Server 系列
            case DbType.SqlServer:
                columnInfo.DataType = "datetimeoffset";
                break;

            // SQLite
            case DbType.Sqlite:
                columnInfo.DataType = "text";
                break;

            // Oracle 系列
            case DbType.Oracle:
            case DbType.OceanBaseForOracle:
                columnInfo.DataType = "timestamp with time zone";
                break;

            // PostgreSQL 系列
            case DbType.PostgreSQL:
            case DbType.OpenGauss:
            case DbType.TDSQLForPGODBC:
                columnInfo.DataType = "timestamp with time zone";
                break;

            // 类 Oracle/PostgreSQL 兼容库
            case DbType.GaussDB:
            case DbType.GaussDBNative:
            case DbType.Vastbase:
            case DbType.Xugu:
            case DbType.Doris:
            case DbType.TDSQL:
            case DbType.GoldenDB:
            case DbType.DuckDB:
            case DbType.QuestDB:
            case DbType.Dm:
            case DbType.Kdbndp:
            case DbType.HANA:
            case DbType.DB2:
                columnInfo.DataType = "timestamp";
                break;

            // TDengine 支持 timestamp 类型
            case DbType.TDengine:
                columnInfo.DataType = "timestamp";
                break;

            case DbType.Custom:
            case DbType.OceanBase:
            default:
                columnInfo.DataType = "datetime";
                break;
        }
    }
}
