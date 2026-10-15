// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;

namespace Fast.SqlSugar;

/// <summary>
/// 自增主键记录 Entity 基类接口
/// </summary>
[SuppressSniffer]
public interface IBaseRecordEntity : IDatabaseEntity
{
    /// <summary>
    /// 设备
    /// </summary>
    string Device { get; set; }

    /// <summary>
    /// 操作系统（版本）
    /// </summary>
    string OS { get; set; }

    /// <summary>
    /// 浏览器（版本）
    /// </summary>
    string Browser { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    string Province { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    string City { get; set; }

    /// <summary>
    /// Ip
    /// </summary>
    string Ip { get; set; }

    /// <summary>
    /// 创建者用户Id
    /// </summary>
    long? CreatedUserId { get; set; }

    /// <summary>
    /// 创建者用户名称
    /// </summary>
    string CreatedUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime? CreatedTime { get; set; }

    /// <summary>
    /// 记录表创建
    /// </summary>
    /// <param name="httpContext">当前请求上下文；非请求环境中可以为 <see langword="null"/></param>
    void RecordCreate(HttpContext httpContext);
}
