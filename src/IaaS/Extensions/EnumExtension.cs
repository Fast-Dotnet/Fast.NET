// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="Enum"/> 提供扩展方法
/// </summary>
public static class EnumExtension
{
    /// <summary>
    /// 获取枚举值的描述
    /// </summary>
    /// <remarks>需要有 [Description] 特性，否则返回的是枚举值的 Name</remarks>
    /// <exception cref="ArgumentNullException">传入的枚举值为空</exception>
    /// <exception cref="ArgumentException">The parameter is not an enum type</exception>
    /// <param name="value">要转换为枚举的名称或数值</param>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <returns>枚举的 [Description] 特性描述</returns>
    public static string GetDescription<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        return value.GetDescription(typeof(TEnum));
    }

    /// <summary>
    /// 获取枚举值的描述
    /// </summary>
    /// <remarks>需要有 [Description] 特性，否则返回的是枚举值的 Name</remarks>
    /// <exception cref="ArgumentNullException">传入的枚举值为空</exception>
    /// <exception cref="ArgumentException">The parameter is not an enum type</exception>
    /// <param name="value">Enum 枚举值</param>
    /// <param name="enumType">enum 类型</param>
    /// <returns>枚举的 [Description] 特性描述</returns>
    public static string GetDescription(this Enum value, Type enumType)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (enumType == null)
        {
            throw new ArgumentNullException(nameof(enumType));
        }

        // 检查是否是枚举类型
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("The parameter is not an enum type.", nameof(value));
        }

        if (!Enum.IsDefined(enumType, value))
        {
            throw new ArgumentException("传入的枚举值不属于指定枚举类型。", nameof(value));
        }

        // 获取枚举名称
        string enumName = Enum.GetName(enumType, value);

        if (enumName is null)
        {
            throw new InvalidOperationException("无法获取枚举成员名称。");
        }

        // 获取枚举字段
        FieldInfo enumField = enumType.GetField(enumName);

        if (enumField is null)
        {
            throw new InvalidOperationException($"无法获取枚举成员“{enumName}”的字段信息。");
        }

        // 获取 [Description] 特性描述
        return enumField.GetCustomAttribute<DescriptionAttribute>(false)?.Description ?? enumName;
    }

    /// <summary>
    /// 将枚举转成枚举信息集合
    /// </summary>
    /// <exception cref="ArgumentException">类型不是一个枚举类型</exception>
    /// <param name="enumType">enum 类型</param>
    /// <returns>将枚举转成枚举信息集合集合</returns>
    public static List<EnumItem<int>> EnumToList(this Type enumType)
    {
        return enumType.EnumToList<int>();
    }

    /// <summary>
    /// 将枚举转成枚举信息集合
    /// </summary>
    /// <exception cref="ArgumentException">类型不是一个枚举类型</exception>
    /// <param name="enumType">enum 类型</param>
    /// <typeparam name="TProperty">属性值类型</typeparam>
    /// <returns>将枚举转成枚举信息集合集合</returns>
    public static List<EnumItem<TProperty>> EnumToList<TProperty>(this Type enumType)
        where TProperty : struct, IComparable, IConvertible, IFormattable
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("Type '" + enumType.Name + "' is not an enum.", nameof(enumType));
        }

        Type propertyType = typeof(TProperty);

        return Enum.GetValues(enumType)
            .Cast<Enum>()
            .Select(enumValue => new EnumItem<TProperty>
            {
                Name = enumValue.ToString(),
                Describe = enumValue.GetDescription(enumType),
                Value = (TProperty)Convert.ChangeType(enumValue, propertyType)
            })
            .ToList();
    }
}
