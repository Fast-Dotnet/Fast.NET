// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

// ReSharper disable once CheckNamespace

namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// 验证 <see cref="Enum"/> 类型属性必填
/// </summary>
/// <remarks>默认验证当前值是否在枚举值中，如果需要验证其余的逻辑，请自行验证</remarks>
[SuppressSniffer]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EnumRequiredAttribute : ValidationAttribute
{
    /// <summary>
    /// 允许零
    /// </summary>
    /// <remarks>常用于默认的枚举值为 None = 0，但是不允许选择的问题</remarks>
    public bool AllowZero { get; set; } = false;

    /// <summary>
    /// Flag 枚举
    /// </summary>
    public bool FlagEnum { get; set; } = false;

    /// <inheritdoc />
    public override bool IsValid(object value)
    {
        if (value == null)
        {
            return false;
        }

        Type type = value.GetType();

        // 判断是否为枚举类型
        if (!type.IsEnum)
        {
            return false;
        }

        try
        {
            // 转为 long 类型，不使用 int 是因为可能会有 long 类型的枚举
            long longVal = Convert.ToInt64(value);

            // 判断是否允许为 0
            if (!AllowZero && longVal == 0)
            {
                return false;
            }

            if (!FlagEnum)
            {
                // 判断值是否为正常的枚举值
                return Enum.IsDefined(type, value);
            }

            long mask = 0L;
            foreach (object item in Enum.GetValues(type))
            {
                mask |= Convert.ToInt64(item);
            }

            return (longVal & ~mask) == 0;
        }
        catch
        {
            return false;
        }
    }
}
