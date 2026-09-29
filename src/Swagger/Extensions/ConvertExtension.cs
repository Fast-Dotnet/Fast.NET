// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.Swagger;

/// <summary>
/// 为 <see cref="Convert"/> 提供转换扩展方法
/// </summary>
internal static class ConvertExtension
{
    /// <summary>
    /// 将 DateTimeOffset 转换成本地 DateTime
    /// </summary>
    /// <param name="dateTime">要转换的日期时间偏移值</param>
    /// <returns>将 DateTimeOffset 转换成本地 DateTime</returns>
    public static DateTime ParseToDateTime(this DateTimeOffset dateTime)
    {
        if (dateTime.Offset.Equals(TimeSpan.Zero))
        {
            return dateTime.UtcDateTime;
        }

        if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
        {
            return dateTime.ToLocalTime().DateTime;
        }

        return dateTime.DateTime;
    }
}
