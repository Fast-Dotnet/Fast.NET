// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

// ReSharper disable once CheckNamespace

namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// 验证 <see cref="string"/> 类型属性必填
/// </summary>
[SuppressSniffer]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class StringRequiredAttribute : ValidationAttribute
{
    /// <summary>
    /// 允许空字符串
    /// </summary>
    public bool AllowEmptyString { get; set; } = false;

    /// <summary>
    /// 允许前后空格
    /// </summary>
    public bool AllowWhitespace { get; set; } = true;

    /// <inheritdoc />
    public override bool IsValid(object value)
    {
        string valueParse = value?.ToString();

        if (valueParse == null)
        {
            return false;
        }

        // 不允许前后空格
        if (!AllowWhitespace)
        {
            int sourceLength = valueParse.Length;
            valueParse = valueParse.TrimStart().TrimEnd();
            if (valueParse.Length != sourceLength)
            {
                throw new ValidationException($"\"{ErrorMessageResourceName}\" 值的前后不能包含空字符串。");
            }
        }

        // 不允许空字符串
        if (!AllowEmptyString)
        {
            if (string.IsNullOrWhiteSpace(valueParse))
            {
                return false;
            }
        }

        return true;
    }
}
