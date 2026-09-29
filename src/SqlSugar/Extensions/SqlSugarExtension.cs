// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections;
using System.Data;
using System.Reflection;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 为 <see cref="ISqlSugarClient"/> 提供 SqlSugar 扩展方法
/// </summary>
[SuppressSniffer]
public static class SqlSugarExtension
{
    /// <summary>
    /// 获取 SugarTable 特性中的 TableName
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>获取到的 SugarTable 特性中的 TableName</returns>
    public static string GetSugarTableName(this Type type)
    {
        SugarTable sugarTable = type.GetCustomAttribute<SugarTable>(true);
        if (sugarTable != null && !string.IsNullOrEmpty(sugarTable.TableName))
        {
            return sugarTable.TableName;
        }

        return type.Name;
    }

    /// <summary>
    /// 获取 SugarTable 特性
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>获取到的 SugarTable 特性</returns>
    public static SugarTable GetSugarTableAttribute(this Type type)
    {
        return type.GetCustomAttribute<SugarTable>(true);
    }

    /// <summary>
    /// 转为 DataTable
    /// </summary>
    /// <param name="list">要处理的集合</param>
    /// <typeparam name="T">数据表行对应的模型类型</typeparam>
    /// <returns>转为 DataTable 集合</returns>
    public static List<DataTable> ToDataTable<T>(this List<T> list)
    {
        var result = new List<DataTable>();

        if (list == null || !list.Any())
        {
            return result;
        }

        Type type = typeof(T);
        if (type.Name == "Object")
        {
            type = list[0].GetType();
        }

        PropertyInfo[] properties = type.GetProperties();
        foreach (T item in list)
        {
            var dataTable = new DataTable();

            // 表名赋值
            dataTable.TableName = type.GetSugarTableName();

            var tempList = new ArrayList();

            foreach (PropertyInfo property in properties)
            {
                Type colType = property.PropertyType;
                // 泛型
                if (colType.IsGenericType && colType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    colType = colType.GetGenericArguments()[0];
                }

                // 获取 Sugar 列特性
                SugarColumn sugarColumn = property.GetCustomAttribute<SugarColumn>(true);

                // 判断忽略列
                if (sugarColumn?.IsIgnore == true)
                {
                    continue;
                }

                string columnName = sugarColumn?.ColumnName ?? property.Name;

                dataTable.Columns.Add(columnName, colType);

                tempList.Add(property.GetValue(item, null));
            }

            dataTable.LoadDataRow(tempList.ToArray(), true);

            result.Add(dataTable);
        }

        return result;
    }
}
