// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="string"/> 提供扩展方法
/// </summary>
public static class StringExtension
{
    /// <summary>
    /// 字符串首字母大写
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>字符串首字母大写</returns>
    public static string FirstCharToUpper(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        var stringBuilder = new StringBuilder(str);

        stringBuilder[0] = char.ToUpperInvariant(stringBuilder[0]);

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 字符串首字母小写
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>字符串首字母小写</returns>
    public static string FirstCharToLower(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        var stringBuilder = new StringBuilder(str);

        stringBuilder[0] = char.ToLowerInvariant(stringBuilder[0]);

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 切割骆驼命名式字符串
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>切割骆驼命名式字符串集合</returns>
    public static string[] SplitCamelCase(this string str)
    {
        if (str == null)
        {
            return Array.Empty<string>();
        }

        if (string.IsNullOrWhiteSpace(str))
        {
            return new[] {str};
        }

        if (str.Length == 1)
        {
            return new[] {str};
        }

        return Regex.Split(str, @"(?=\p{Lu}\p{Ll})|(?<=\p{Ll})(?=\p{Lu})").Where(u => u.Length > 0).ToArray();
    }

    /// <summary>
    /// 字符串小驼峰命名
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>字符串小驼峰命名</returns>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        string[] arr = str.SplitCamelCase();

        if (arr.Length == 1)
        {
            return str.ToLower();
        }

        return $"{arr[0].ToLower()}{string.Join(string.Empty, arr.Skip(1))}";
    }

    /// <summary>
    /// 清除字符串前后缀
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="pos">开始处理的位置</param>
    /// <param name="affixes">要移除或保留的前后缀集合</param>
    /// <returns>清除字符串前后缀</returns>
    public static string ClearStringAffixes(this string str, int pos = 0, params string[] affixes)
    {
        // 空字符串直接返回
        if (string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        // 空前后缀集合直接返回
        if (affixes == null || affixes.Length == 0)
        {
            return str;
        }

        bool startCleared = false;
        bool endCleared = false;

        string tempStr = null;
        foreach (string affix in affixes)
        {
            if (string.IsNullOrWhiteSpace(affix))
            {
                continue;
            }

            if (pos != 1 && !startCleared && str.StartsWith(affix, StringComparison.OrdinalIgnoreCase))
            {
                tempStr = str[affix.Length..];
                startCleared = true;
            }

            if (pos != -1 && !endCleared && str.EndsWith(affix, StringComparison.OrdinalIgnoreCase))
            {
                string _tempStr = !string.IsNullOrWhiteSpace(tempStr) ? tempStr : str;
                tempStr = _tempStr[..^affix.Length];
                endCleared = true;
            }

            if (startCleared && endCleared)
            {
                break;
            }
        }

        return !string.IsNullOrWhiteSpace(tempStr) ? tempStr : str;
    }

    /// <summary>
    /// 格式化字符串
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="args">格式化消息时使用的参数</param>
    /// <returns>格式化字符串</returns>
    public static string Format(this string str, params object[] args)
    {
        return args == null || args.Length == 0 ? str : string.Format(str, args);
    }

    /// <summary>
    /// 获取字符长度
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>获取到的字符长度</returns>
    public static int GetCharLength(this string str)
    {
        return Encoding.GetEncoding("GB18030").GetByteCount(str);
    }

    /// <summary>
    /// 将字符串转化为固定长度左对齐，右补空格
    /// </summary>
    /// <param name="strTemp">递归处理过程中已累积的字符串</param>
    /// <param name="length">目标长度</param>
    /// <returns>将字符串转化为固定长度左对齐，右补空格</returns>
    public static string PadStringLeftAlign(this string strTemp, int length)
    {
        strTemp ??= string.Empty;
        strTemp = strTemp.Trim();
        int iLength = strTemp.GetCharLength();
        int padCount = length - iLength;
        // 当前显示宽度已达到目标值时不再填充
        if (padCount <= 0)
        {
            return strTemp;
        }

        return strTemp + new string(' ', padCount);
    }

    /// <summary>
    /// 将字符串转化为固定长度右对齐，左补空格
    /// </summary>
    /// <param name="strTemp">递归处理过程中已累积的字符串</param>
    /// <param name="length">目标长度</param>
    /// <returns>将字符串转化为固定长度右对齐，左补空格</returns>
    public static string PadStringRightAlign(this string strTemp, int length)
    {
        strTemp ??= string.Empty;
        strTemp = strTemp.Trim();
        int iLength = strTemp.GetCharLength();
        int padCount = length - iLength;
        // 当前显示宽度已达到目标值时不再填充
        if (padCount <= 0)
        {
            return strTemp;
        }

        return new string(' ', padCount) + strTemp;
    }

    /// <summary>
    /// 将字符串转换为固定长度的数组
    /// </summary>
    /// <param name="strTemp">递归处理过程中已累积的字符串</param>
    /// <param name="length">目标长度</param>
    /// <returns>将字符串转换为固定长度的数组集合</returns>
    public static List<string> SplitString(this string strTemp, int length)
    {
        strTemp ??= string.Empty;
        strTemp = strTemp.Trim();
        var encoding = Encoding.GetEncoding("GB18030");
        var result = new List<string>();
        var sb = new StringBuilder();
        int curLen = 0;
        foreach (char c in strTemp)
        {
            int cLen = encoding.GetByteCount(new[] {c});
            if (curLen + cLen > length)
            {
                result.Add(sb.ToString());
                sb.Clear();
                curLen = 0;
            }

            sb.Append(c);
            curLen += cLen;
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result;
    }

    /// <summary>
    /// 截取指定长度的字符串
    /// </summary>
    /// <param name="value">要处理的字符串</param>
    /// <param name="length">目标长度</param>
    /// <param name="ellipsis">文本被截断时追加的省略标记</param>
    /// <returns>截取指定长度的字符串</returns>
    public static string GetSubStringWithEllipsis(this string value, int length, bool ellipsis = false)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length <= length)
        {
            return value;
        }

        value = value[..length];
        if (ellipsis)
        {
            value += "...";
        }

        return value;
    }

    /// <summary>
    /// 获取 SQL Server NVarchar 最大字节长度
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="maxLen">目标长度</param>
    /// <param name="ellipsis">文本被截断时追加的省略标记</param>
    /// <returns>获取到的 SQL Server NVarchar 最大字节长度</returns>
    public static string GetNVarcharMaxLen(this string str, int maxLen, bool ellipsis = false)
    {
        // NVARCHAR 每个字符占用 2 个字节
        int maxByteLen = maxLen * 2;
        int byteLen = Encoding.Unicode.GetBytes(str).Length;

        if (byteLen <= maxLen)
        {
            // 长度符合
            return str;
        }

        // 判断是否需要省略号
        int maxCharLen;
        if (ellipsis)
        {
            // 考虑省略号的字节长度为 6
            maxCharLen = (maxByteLen - 6) / 2;
        }
        else
        {
            maxCharLen = maxByteLen;
        }

        return str.GetSubStringWithEllipsis(maxCharLen, ellipsis);
    }
}
