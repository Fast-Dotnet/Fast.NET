// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Fast.IaaS;

/// <summary>
/// 提供字符串编码扩展方法
/// </summary>
public static class StringEncodingExtension
{
    #region ASCII

    /// <summary>
    /// 将字符串转换为 ASCII 编码形式
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>转换后的 ASCII 编码形式字符串</returns>
    public static string EnAscii(this string str)
    {
        // 使用 UTF-8 编码将字符串转换为字节数组
        byte[] textBuf = Encoding.UTF8.GetBytes(str);

        // 将每个字节转换为两位的十六进制数，并拼接起来
        return textBuf.Aggregate(string.Empty, (current, t) => current + t.ToString("X"));
    }

    /// <summary>
    /// 将 ASCII 编码形式的字符串转换为字符串
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>将 ASCII 编码形式的字符串转换为字符串</returns>
    public static string DeAscii(this string str)
    {
        int k = 0;
        // 创建一个字节数组，长度为输入字符串长度的一半
        byte[] buffer = new byte[str.Length / 2];
        for (int i = 0; i < str.Length / 2; i++)
        {
            // 从 ASCII 编码形式的字符串中提取两位十六进制数，将其转换为字节
            buffer[i] = byte.Parse(str.Substring(k, 2), NumberStyles.HexNumber);
            k += 2;
        }

        // 使用 UTF-8 编码将字节数组转换为字符串
        return Encoding.UTF8.GetString(buffer);
    }

    #endregion

    #region Unicode

    /// <summary>
    /// Unicode 编码
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>Unicode 编码</returns>
    public static string EnUnicode(this string str)
    {
        var strResult = new StringBuilder();
        if (string.IsNullOrEmpty(str))
        {
            return strResult.ToString();
        }

        foreach (char c in str)
        {
            strResult.Append("\\u");
            strResult.Append(((int)c).ToString("x"));
        }

        return strResult.ToString();
    }

    /// <summary>
    /// Unicode 解码
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>Unicode 解码</returns>
    public static string DeUnicode(this string str)
    {
        //最直接的方法 Regex.Unescape(str)
        var reg = new Regex(@"(?i)\\[uU]([0-9a-f]{4})");
        return reg.Replace(str, m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }

    #endregion

    #region Url

    /// <summary>
    /// 对字符串进行 URL 编码；已编码的字符串保持不变
    /// </summary>
    /// <param name="str">要编码的字符串</param>
    /// <returns>URL 编码后的字符串；输入为空时返回空字符串</returns>
    public static string UrlEncode(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return "";
        }

        string result = HttpUtility.UrlEncode(str, Encoding.UTF8);

        try
        {
            // 尝试解密，避免再次编码已经是 URL 编码的字符串
            string tryDecode = HttpUtility.UrlDecode(str, Encoding.UTF8);
            // 如果解码后不相同，则直接返回原来的
            return str.Equals(tryDecode, StringComparison.OrdinalIgnoreCase) ? result : str;
        }
        catch
        {
            // 无法可靠判断原字符串是否已编码时，返回本次编码结果
            return result;
        }
    }

    /// <summary>
    /// 将一个 URL 编码 转为字符串
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <returns>将一个 URL 编码 转为字符串</returns>
    public static string UrlDecode(this string str)
    {
        return string.IsNullOrEmpty(str) ? "" : HttpUtility.UrlDecode(str, Encoding.UTF8);
    }

    #endregion
}
