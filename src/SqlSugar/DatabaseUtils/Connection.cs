// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 连接字符串工具类
/// </summary>
internal partial class SqlSugarDatabaseUtil
{
    /// <summary>
    /// 得到数据库连接字符串
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="dbInfo">数据库连接与类型信息</param>
    /// <returns>得到数据库连接字符串</returns>
    internal static string GetConnectionStr(DbType dbType, SlaveConnectionInfo dbInfo)
    {
        return dbType switch
        {
            DbType.MySql or DbType.MySqlConnector =>
                $"Server={dbInfo.ServiceIp};Port={dbInfo.Port ?? 3306};Database={dbInfo.DbName};User ID={dbInfo.DbUser};Password={dbInfo.DbPwd};CharSet=utf8;SslMode=none;Pooling=true;Convert Zero Datetime=True;Allow Zero Datetime=True;Max Pool Size={dbInfo.MaxPoolSize ?? 100};",
            DbType.SqlServer =>
                $"Server={dbInfo.ServiceIp},{dbInfo.Port ?? 1433};Database={dbInfo.DbName};User={dbInfo.DbUser};Password={dbInfo.DbPwd};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;Max Pool Size={dbInfo.MaxPoolSize ?? 100};",
            DbType.Sqlite => $"Data Source={dbInfo.DbName};",
            DbType.Oracle =>
                $"Data Source={dbInfo.ServiceIp}:{dbInfo.Port ?? 1521}/{dbInfo.DbName};User ID={dbInfo.DbUser};Password={dbInfo.DbPwd};",
            DbType.PostgreSQL =>
                $"Host={dbInfo.ServiceIp};Port={dbInfo.Port ?? 5432};Database={dbInfo.DbName};Username={dbInfo.DbUser};Password={dbInfo.DbPwd};Pooling=true;MaxPoolSize={dbInfo.MaxPoolSize ?? 100};",
            DbType.Access => $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbInfo.DbName};Persist Security Info=False;",
            DbType.MongoDb =>
                $"mongodb://{dbInfo.DbUser}:{dbInfo.DbPwd}@{dbInfo.ServiceIp}:{dbInfo.Port ?? 27017}/{dbInfo.DbName}",
            _ => throw new SqlSugarException("暂不支持此数据库类型连接字符串配置！")
        };
    }

    /// <summary>
    /// 得到数据库连接字符串
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <param name="dbInfo">数据库连接与类型信息</param>
    /// <returns>得到数据库连接字符串</returns>
    internal static string GetConnectionStr(DbType dbType, ConnectionSettingsOptions dbInfo)
    {
        string connectionStr;
        if (!string.IsNullOrWhiteSpace(dbInfo.CustomConnectionStr))
        {
            connectionStr = dbInfo.CustomConnectionStr;
        }
        else
        {
            connectionStr = dbType switch
            {
                DbType.MySql or DbType.MySqlConnector =>
                    $"Server={dbInfo.ServiceIp};Port={dbInfo.Port ?? 3306};Database={dbInfo.DbName};User ID={dbInfo.DbUser};Password={dbInfo.DbPwd};CharSet=utf8;SslMode=none;Pooling=true;Convert Zero Datetime=True;Allow Zero Datetime=True;Max Pool Size={dbInfo.MaxPoolSize ?? 100};",
                DbType.SqlServer =>
                    $"Server={dbInfo.ServiceIp},{dbInfo.Port ?? 1433};Database={dbInfo.DbName};User={dbInfo.DbUser};Password={dbInfo.DbPwd};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=True;Max Pool Size={dbInfo.MaxPoolSize ?? 100};",
                DbType.Sqlite => $"Data Source={dbInfo.DbName};",
                DbType.Oracle =>
                    $"Data Source={dbInfo.ServiceIp}:{dbInfo.Port ?? 1521}/{dbInfo.DbName};User ID={dbInfo.DbUser};Password={dbInfo.DbPwd};",
                DbType.PostgreSQL =>
                    $"Host={dbInfo.ServiceIp};Port={dbInfo.Port ?? 5432};Database={dbInfo.DbName};Username={dbInfo.DbUser};Password={dbInfo.DbPwd};Pooling=true;MaxPoolSize={dbInfo.MaxPoolSize ?? 100};",
                DbType.Access => $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbInfo.DbName};Persist Security Info=False;",
                DbType.MongoDb =>
                    $"mongodb://{dbInfo.DbUser}:{dbInfo.DbPwd}@{dbInfo.ServiceIp}:{dbInfo.Port ?? 27017}/{dbInfo.DbName}",
                _ => throw new SqlSugarException("暂不支持此数据库类型连接字符串配置！")
            };
        }

        return connectionStr;
    }
}
