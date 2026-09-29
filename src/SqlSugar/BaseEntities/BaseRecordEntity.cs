// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel.DataAnnotations;
using Fast.Runtime;
using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 记录 Entity 基类
/// </summary>
[SuppressSniffer]
public class BaseRecordEntity : IBaseRecordEntity
{
    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "设备", Length = 50, IsNullable = true, CreateTableFieldSort = 983)]
    public virtual string Device { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "操作系统（版本）", Length = 50, IsNullable = true, CreateTableFieldSort = 984)]
    public virtual string OS { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "浏览器（版本）", Length = 50, IsNullable = true, CreateTableFieldSort = 985)]
    public virtual string Browser { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "省份", Length = 20, IsNullable = true, CreateTableFieldSort = 986)]
    public virtual string Province { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "城市", Length = 20, IsNullable = true, CreateTableFieldSort = 987)]
    public virtual string City { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "Ip", Length = 15, IsNullable = true, CreateTableFieldSort = 988)]
    public virtual string Ip { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "部门Id", CreateTableFieldSort = 989)]
    public virtual long? DepartmentId { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "部门名称", Length = 20, IsNullable = true, CreateTableFieldSort = 990)]
    public virtual string DepartmentName { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "创建者用户Id", CreateTableFieldSort = 991)]
    public virtual long? CreatedUserId { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "创建者用户名称", Length = 20, IsNullable = true, CreateTableFieldSort = 992)]
    public virtual string CreatedUserName { get; set; }

    /// <inheritdoc />
    [Required]
    [SugarColumn(ColumnDescription = "创建时间", CreateTableFieldSort = 993)]
    public virtual DateTime? CreatedTime { get; set; }

    /// <inheritdoc />
    public void RecordCreate(HttpContext httpContext)
    {
        UserAgentInfo userAgentInfo = httpContext.RequestUserAgentInfo();
        WanNetIPInfo wanInfo = httpContext.RemoteIpv4Info();

        Device = userAgentInfo?.Device;
        OS = userAgentInfo?.OS;
        Browser = userAgentInfo?.Browser;
        Province = wanInfo?.Province;
        City = wanInfo?.City;
        Ip = wanInfo?.Ip;
    }
}
