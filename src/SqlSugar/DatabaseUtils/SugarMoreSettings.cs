// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SugarMoreSettings 工具类
/// </summary>
internal partial class SqlSugarDatabaseUtil
{
    /// <summary>
    /// 获取配置
    /// </summary>
    /// <returns>获取到的配置</returns>
    internal static ConnMoreSettings GetSugarMoreSettings()
    {
        var moreSettings = new ConnMoreSettings
        {
            // SQL Server Code First 默认使用 NVARCHAR
            SqlServerCodeFirstNvarchar = true,
            // 最小时间
            DbMinDate = new DateTime(1970, 01, 01),
            // CodeFirst 启用 Sqlite 列删除
            SqliteCodeFirstEnableDropColumn = true,
            // CodeFirst 启用精度修改
            EnableCodeFirstUpdatePrecision = true
        };
        return moreSettings;
    }
}
