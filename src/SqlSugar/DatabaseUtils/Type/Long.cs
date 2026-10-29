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
    /// 设置 <see cref="long"/> 类型
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="columnInfo">要补充数据库类型信息的实体列元数据</param>
    internal static void SetDbTypeLong(DbType dbType, EntityColumnInfo columnInfo)
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
                columnInfo.DataType = "bigint";
                break;

            // SQL Server
            case DbType.SqlServer:
                columnInfo.DataType = "bigint";
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
                columnInfo.DataType = "bigint";
                break;

            // Oracle 系列
            case DbType.Oracle:
            case DbType.OceanBaseForOracle:
                columnInfo.DataType = "number(19,0)";
                break;

            // SQLite
            case DbType.Sqlite:
                columnInfo.DataType = "integer";
                break;

            // ClickHouse
            case DbType.ClickHouse:
                columnInfo.DataType = "int64";
                break;

            // QuestDB
            case DbType.QuestDB:
                columnInfo.DataType = "long";
                break;

            // MongoDB
            case DbType.MongoDb:
                columnInfo.DataType = "int64";
                break;

            // Access
            case DbType.Access:
                columnInfo.DataType = "long";
                break;

            // 其他类关系型数据库统一用 bigint
            case DbType.Dm:
            case DbType.Oscar:
            case DbType.Kdbndp:
            case DbType.DB2:
            case DbType.HANA:
            case DbType.Odbc:
                columnInfo.DataType = "bigint";
                break;

            // TDengine 明确不支持 long/int64，需避免使用
            case DbType.TDengine:
                throw new SqlSugarException("TDengine 不支持 long 类型，请使用 bigint 或 double 替代。");

            // OceanBase（MySQL 模式）
            case DbType.OceanBase:
                columnInfo.DataType = "bigint";
                break;

            case DbType.Custom:
            default:
                columnInfo.DataType = "bigint";
                break;
        }
    }
}
