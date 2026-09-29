// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="object"/> 提供扩展方法
/// </summary>
public static class ObjectExtension
{
    /// <summary>
    /// 将一个对象转换为指定类型
    /// </summary>
    /// <param name="obj">要处理的对象</param>
    /// <param name="type">目标类型</param>
    /// <returns>将一个对象转换为指定类型</returns>
    public static object ChangeType(this object obj, Type type)
    {
        if (type == null)
        {
            return obj;
        }

        if (type == typeof(string))
        {
            return obj?.ToString();
        }

        if (type == typeof(Guid) && obj != null)
        {
            return Guid.Parse(obj.ToString());
        }

        if (type == typeof(bool) && obj != null && !(obj is bool))
        {
            string objStr = obj.ToString()?.ToLower();
            if (objStr == "1" || objStr == "true" || objStr == "yes" || objStr == "on")
            {
                return true;
            }

            return false;
        }

        if (obj == null)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        Type underlyingType = Nullable.GetUnderlyingType(type);
        if (type.IsInstanceOfType(obj))
        {
            return obj;
        }

        if ((underlyingType ?? type).IsEnum)
        {
            if (underlyingType != null && string.IsNullOrWhiteSpace(obj.ToString()))
            {
                return null;
            }

            return Enum.Parse(underlyingType ?? type, obj.ToString());
        }
        // 将 DateTime 按配置的时区规则转换为 DateTimeOffset

        if (obj is DateTime dateTime && (underlyingType ?? type) == typeof(DateTimeOffset))
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
        }
        // 将 DateTimeOffset 按配置的时区规则转换为 DateTime

        if (obj is DateTimeOffset dateTimeOffset && (underlyingType ?? type) == typeof(DateTime))
        {
            return dateTimeOffset.ParseToDateTime();
        }

        if (typeof(IConvertible).IsAssignableFrom(underlyingType ?? type))
        {
            try
            {
                return Convert.ChangeType(obj, underlyingType ?? type, null);
            }
            catch
            {
                return underlyingType == null ? Activator.CreateInstance(type) : null;
            }
        }

        TypeConverter converter = TypeDescriptor.GetConverter(type);
        if (converter.CanConvertFrom(obj.GetType()))
        {
            return converter.ConvertFrom(obj);
        }

        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor != null)
        {
            object o = constructor.Invoke(null);
            PropertyInfo[] propertyArr = type.GetProperties();
            Type oldType = obj.GetType();

            foreach (PropertyInfo property in propertyArr)
            {
                PropertyInfo p = oldType.GetProperty(property.Name);
                if (property.CanWrite && p != null && p.CanRead)
                {
                    property.SetValue(o, p.GetValue(obj, null).ChangeType(property.PropertyType), null);
                }
            }

            return o;
        }

        return obj;
    }

    /// <summary>
    /// 将一个 Object 对象转为 字典
    /// </summary>
    /// <param name="obj">要处理的对象</param>
    /// <param name="includeNull">是否在结果中包含值为 <see langword="null"/> 的属性</param>
    /// <returns>IDictionary{TKey,TValue}</returns>
    public static IDictionary<string, object> ToDictionary(this object obj, bool includeNull = false)
    {
        var dictionary = new Dictionary<string, object>();

        Type t = obj.GetType(); // 获取对象对应的类， 对应的类型

        PropertyInfo[] pi = t.GetProperties(BindingFlags.Public | BindingFlags.Instance); // 获取当前type公共属性

        foreach (PropertyInfo p in pi)
        {
            MethodInfo m = p.GetGetMethod();

            if (m == null || !m.IsPublic)
            {
                continue;
            }

            object o = m.Invoke(obj, Array.Empty<object>());
            if (o != null || includeNull)
            {
                dictionary.Add(p.Name, o); // 向字典添加元素
            }
        }

        return dictionary;
    }

    /// <summary>
    /// 将一个对象转换为指定类型
    /// </summary>
    /// <param name="obj">要处理的对象</param>
    /// <typeparam name="T">转换操作使用的对象类型</typeparam>
    /// <returns>将一个对象转换为指定类型</returns>
    public static T ChangeType<T>(this object obj)
    {
        return (T)obj.ChangeType(typeof(T));
    }

    /// <summary>
    /// 将对象的公共属性转换为查询字符串
    /// </summary>
    /// <remarks>字符串和整数列表会使用带 <c>[]</c> 后缀的重复查询参数；其他复杂属性使用其字符串表示形式</remarks>
    /// <param name="obj">要转换的对象</param>
    /// <param name="isToLower">是否将属性名的首字母转换为小写</param>
    /// <returns>由非空公共属性组成的查询字符串；对象为空时返回空字符串</returns>
    public static string ToQueryString(this object obj, bool isToLower = false)
    {
        if (obj == null)
        {
            return string.Empty;
        }

        var dictionary = new Dictionary<string, string>();

        Type t = obj.GetType(); // 获取对象对应的类， 对应的类型

        PropertyInfo[] pi = t.GetProperties(BindingFlags.Public | BindingFlags.Instance); // 获取当前type公共属性

        foreach (PropertyInfo p in pi)
        {
            MethodInfo m = p.GetGetMethod();

            if (m == null || !m.IsPublic)
            {
                continue;
            }

            if (m.Invoke(obj, new object[] { }) == null)
            {
                continue;
            }

            object value = m.Invoke(obj, new object[] { });

            // 进行 List 集合处理
            Type valType = value?.GetType();
            if (valType is {IsGenericType: true})
            {
                // 这里如果还有别的参数，需要再次添加
                switch (value)
                {
                    case List<string> strList:
                        string strListVal = strList.Aggregate("",
                            (current, item) => current + $"{item}&{(isToLower ? p.Name.FirstCharToLower() : p.Name)}[]=");

                        strListVal = strListVal[..^$"&{(isToLower ? p.Name.FirstCharToLower() : p.Name)}[]=".Length];

                        dictionary.Add($"{(isToLower ? p.Name.FirstCharToLower() : p.Name)}[]", strListVal); // 向字典添加元素
                        break;
                    case List<int> intList:
                        string intListVal = intList.Aggregate("",
                            (current, item) => current + $"{item}&{(isToLower ? p.Name.FirstCharToLower() : p.Name)}[]=");

                        intListVal = intListVal[..^$"&{(isToLower ? p.Name.FirstCharToLower() : p.Name)}[]=".Length];

                        dictionary.Add($"{(isToLower ? p.Name.FirstCharToLower() : p.Name)}[]", intListVal); // 向字典添加元素
                        break;
                    default:
                        dictionary.Add(p.Name, m.Invoke(obj, new object[] { })?.ToString()); // 向字典添加元素
                        break;
                }
            }
            else
            {
                dictionary.Add(p.Name, m.Invoke(obj, new object[] { })?.ToString()); // 向字典添加元素
            }
        }

        return dictionary.ToQueryString(isToLower: isToLower);
    }

    /// <summary>
    /// 尝试获取对象的数量
    /// </summary>
    /// <param name="obj">要处理的对象</param>
    /// <param name="count">得到的元素数量</param>
    /// <returns>能够读取字符、字符串、集合或公开 <c>Count</c> 属性的数量时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool TryGetCount(this object obj, out int count)
    {
        // 处理可直接获取长度的类型

        // 检查对象是否是字符类型
        if (obj is char)
        {
            count = 1;
            return true;
        }
        // 检查对象是否是字符串类型

        if (obj is string text)
        {
            count = text.Length;
            return true;
        }
        // 检查对象是否实现了 ICollection 接口

        if (obj is ICollection collection)
        {
            count = collection.Count;
            return true;
        }

        // 反射查找是否存在 Count 属性
        PropertyInfo runtimeProperty = obj.GetType().GetRuntimeProperty("Count");

        // 反射获取 Count 属性值
        if (!(runtimeProperty is null) && runtimeProperty.CanRead && runtimeProperty.PropertyType == typeof(int))
        {
            count = (int)runtimeProperty.GetValue(obj)!;
            return true;
        }

        count = -1;
        return false;
    }
}
