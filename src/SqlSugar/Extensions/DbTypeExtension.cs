// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 为 <see cref="DbType"/> 提供扩展方法
/// </summary>
[SuppressSniffer]
public static class DbTypeExtension
{
    /// <summary>
    /// 将 <see cref="SugarDbType"/> 转换为 <see cref="DbType"/>
    /// </summary>
    /// <param name="sugarDbType">sugar Db 类型</param>
    /// <returns>将 SugarDbType 转换为 DbType</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sugarDbType"/> 不是当前支持的六种数据库类型之一。</exception>
    public static DbType ToDbType(this SugarDbType sugarDbType)
    {
        return sugarDbType switch
        {
            SugarDbType.MySql => DbType.MySql,
            SugarDbType.SqlServer => DbType.SqlServer,
            SugarDbType.Sqlite => DbType.Sqlite,
            SugarDbType.Oracle => DbType.Oracle,
            SugarDbType.PostgreSQL => DbType.PostgreSQL,
            SugarDbType.MongoDb => DbType.MongoDb,
            _ => throw new ArgumentOutOfRangeException(nameof(sugarDbType), sugarDbType, "不支持的数据库类型")
        };
    }

    /// <summary>
    /// 将 <see cref="DbType"/> 转换为 <see cref="SugarDbType"/>
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <returns>将 DbType 转换为 SugarDbType</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dbType"/> 不属于 MySql、SqlServer、Sqlite、Oracle、PostgreSQL 或 MongoDb。</exception>
    public static SugarDbType ToSugarDbType(this DbType dbType)
    {
        return dbType switch
        {
            DbType.MySql => SugarDbType.MySql,
            DbType.SqlServer => SugarDbType.SqlServer,
            DbType.Sqlite => SugarDbType.Sqlite,
            DbType.Oracle => SugarDbType.Oracle,
            DbType.PostgreSQL => SugarDbType.PostgreSQL,
            DbType.MongoDb => SugarDbType.MongoDb,
            _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "不支持的数据库类型")
        };
    }
}
