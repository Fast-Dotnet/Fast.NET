// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel;
using System.Reflection;

namespace Fast.Swagger;

/// <summary>
/// 为 <see cref="object"/> 提供扩展方法
/// </summary>
internal static class ObjectExtension
{
    /// <summary>
    /// 将一个对象转换为指定类型
    /// </summary>
    /// <param name="obj">待转换的对象</param>
    /// <param name="type">目标类型</param>
    /// <returns>转换后的对象</returns>
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

        if (type == typeof(bool) && obj != null && obj is not bool)
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
}
