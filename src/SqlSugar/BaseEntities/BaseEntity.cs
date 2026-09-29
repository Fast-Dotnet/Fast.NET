// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// Entity 基类
/// </summary>
[SuppressSniffer]
public class BaseEntity : IBaseEntity
{
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
    [SugarColumn(ColumnDescription = "更新者用户Id", CreateTableFieldSort = 994)]
    public virtual long? UpdatedUserId { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "更新者用户名称", Length = 20, IsNullable = true, CreateTableFieldSort = 995)]
    public virtual string UpdatedUserName { get; set; }

    /// <inheritdoc />
    [SugarColumn(ColumnDescription = "更新时间", CreateTableFieldSort = 996)]
    public virtual DateTime? UpdatedTime { get; set; }
}
