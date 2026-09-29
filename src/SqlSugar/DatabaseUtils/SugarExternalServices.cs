// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// SugarExternalServices 工具类
/// </summary>
public partial class SqlSugarDatabaseUtil
{
    /// <summary>
    /// 目前只验证了 SQL Server 和 MySql
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <returns>目前只验证了 SQL Server 和 MySql</returns>
    internal static ConfigureExternalServices GetSugarExternalServices(DbType dbType)
    {
        var externalServices = new ConfigureExternalServices
        {
            EntityNameService = (type, entityInfo) =>
            {
                // 全局开启创建表按照字段排序，避免重复代码
                entityInfo.IsCreateTableFiledSort = true;

                // Table Name 配置，如果使用 SqlSugar 的规范，其实这里是不会走的
                TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>(true);
                if (tableAttribute != null)
                {
                    entityInfo.DbTableName = tableAttribute.Name;
                }

                // 禁用 CodeFirst 列删除功能
                entityInfo.IsDisabledDelete = true;
            },
            EntityService = (propertyInfo, columnInfo) =>
            {
                // 列名配置，如果使用 SqlSugar 的规范，其实这里是不会走的
                ColumnAttribute columnAttribute = propertyInfo.GetCustomAttribute<ColumnAttribute>(true);
                if (columnAttribute != null)
                {
                    columnInfo.DbColumnName = columnAttribute.Name;
                }

                // 主键配置，如果使用 SqlSugar 的规范，其实这里是不会走的
                KeyAttribute keyAttribute = propertyInfo.GetCustomAttribute<KeyAttribute>(true);
                if (keyAttribute != null)
                {
                    columnInfo.IsPrimarykey = true;
                }

                if (!columnInfo.IsPrimarykey)
                {
                    // 可空类型配置
                    if (propertyInfo.PropertyType.IsValueType)
                    {
                        // 值类型，int? 等等
                        if (propertyInfo.PropertyType.IsGenericType
                            && propertyInfo.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            columnInfo.IsNullable = true;
                        }
                    }
                    else
                    {
                        // 引用类型，string 等等
                        if (new NullabilityInfoContext().Create(propertyInfo).WriteState is NullabilityState.Unknown
                            or NullabilityState.Nullable)
                        {
                            columnInfo.IsNullable = true;
                        }
                    }

                    // 非空类型配置，主要针对 string 类型的必填验证
                    RequiredAttribute requiredAttribute = propertyInfo.GetCustomAttribute<RequiredAttribute>(true);
                    if (requiredAttribute != null)
                    {
                        columnInfo.IsNullable = false;
                    }
                }

                // 这里默认都是 SQL Server 的配置
                if (string.IsNullOrEmpty(columnInfo.DataType))
                {
                    Type propertyType = propertyInfo.PropertyType.IsGenericType
                        ? Nullable.GetUnderlyingType(propertyInfo.PropertyType)
                        : propertyInfo.PropertyType;

                    // 枚举处理
                    if (propertyType?.IsEnum == true)
                    {
                        Type enumType = Enum.GetUnderlyingType(propertyType);

                        columnInfo.DataType = enumType switch
                        {
                            not null when enumType == typeof(byte) => "tinyint",
                            not null when enumType == typeof(short) => "smallint",
                            not null when enumType == typeof(long) => "bigint",
                            _ => "int"
                        };
                    }
                    else if (propertyType == typeof(DateTimeOffset))
                    {
                        columnInfo.DataType = "datetimeoffset";
                    }
                }

                // 这里的所有数据库类型，默认是根据 SqlServer 配置的
                string columnDbType = columnInfo.DataType?.ToLower();
                if (columnDbType == null)
                {
                    return;
                }

                switch (columnDbType)
                {
                    case "tinyint":
                        SetDbTypeByte(dbType, columnInfo);
                        break;
                    case "smallint":
                        SetDbTypeShort(dbType, columnInfo);
                        break;
                    case "bigint":
                        SetDbTypeLong(dbType, columnInfo);
                        break;
                    case "int":
                        SetDbTypeInt(dbType, columnInfo);
                        break;
                    case "datetimeoffset":
                        SetDbTypeDateTime(dbType, columnInfo);
                        break;
                }

                if (columnDbType.StartsWith("varchar("))
                {
                    SetDbTypeVarchar(dbType, columnInfo);
                }
            }
        };
        return externalServices;
    }
}
