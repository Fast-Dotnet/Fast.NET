// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="DataTable"/> 提供扩展方法
/// </summary>
public static class DataTableExtension
{
    /// <summary>
    /// 转换为 DataTable
    /// </summary>
    /// <param name="data">要处理或传输的数据</param>
    /// <typeparam name="T">数据表行对应的模型类型</typeparam>
    /// <returns>转换后的为 DataTable</returns>
    public static DataTable ToDataTable<T>(this IEnumerable<T> data)
    {
        var dataTable = new DataTable();

        // 获取模型类型的属性列表
        PropertyInfo[] properties = typeof(T).GetProperties();

        // 创建 DataTable 的列
        foreach (PropertyInfo prop in properties)
        {
            dataTable.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
        }

        // 将数据添加到 DataTable
        foreach (T item in data)
        {
            object[] values = new object[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                values[i] = properties[i].GetValue(item, null);
            }

            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    /// <summary>
    /// DataTable To List
    /// </summary>
    /// <param name="dataTable">要转换的数据表</param>
    /// <typeparam name="T">数据表行对应的模型类型</typeparam>
    /// <returns>DataTable To List 集合</returns>
    public static List<T> ToList<T>(this DataTable dataTable) where T : new()
    {
        var list = new List<T>();

        foreach (DataRow row in dataTable.Rows)
        {
            var item = new T();

            foreach (DataColumn column in dataTable.Columns)
            {
                PropertyInfo property = typeof(T).GetProperty(column.ColumnName);
                if (property != null && row[column] != DBNull.Value)
                {
                    property.SetValue(item, row[column]);
                }
            }

            list.Add(item);
        }

        return list;
    }
}
