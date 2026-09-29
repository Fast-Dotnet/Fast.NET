// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Fast.IaaS;

/// <summary>
/// 提供 Base64 扩展方法
/// </summary>
public static class Base64Extension
{
    /// <summary>
    /// 随机字符长度
    /// </summary>
    public const int RandomPrefixStrLength = 6;

    private static readonly Encoding encoding = Encoding.UTF8;

    /// <summary>
    /// 普通 字符串 转换为 Base64 字符串
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="randomPrefixStrLength">随机前缀的字符数</param>
    /// <returns>普通 字符串 转换为 Base64 字符串</returns>
    public static string ToBase64(this string str, int randomPrefixStrLength = RandomPrefixStrLength)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return "";
        }

        try
        {
            string randomPrefixStr = VerificationUtil.GenStrVerCode(randomPrefixStrLength);
            byte[] buffer = encoding.GetBytes(str);
            string base64Str = Convert.ToBase64String(buffer);

            base64Str = randomPrefixStrLength == 0 ? base64Str : InsertRandomStrToBase64Str(base64Str);

            return $"{randomPrefixStr}{base64Str}";
        }
        catch (Exception ex)
        {
            TextWriter output = Console.Out;
            lock (output)
            {
                var writer = new ConsoleWriter(output);
                try
                {
                    writer.BackgroundColor = ConsoleColor.DarkRed;
                    writer.ForegroundColor = ConsoleColor.Black;
                    writer.Write("fail");
                    writer.ResetColor();
                    writer.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                    writer.BackgroundColor = ConsoleColor.DarkRed;
                    writer.ForegroundColor = ConsoleColor.Black;
                    writer.WriteLine($"      Base64Util.ToBase64: {ex}");
                }
                finally
                {
                    writer.ResetColor();
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Base64 字符串 转换为 普通 字符串
    /// </summary>
    /// <param name="base64Str">要解码的 Base64 文本</param>
    /// <param name="randomPrefixStrLength">随机前缀的字符数</param>
    /// <returns>Base64 字符串 转换为 普通 字符串</returns>
    public static string Base64ToString(this string base64Str, int randomPrefixStrLength = RandomPrefixStrLength)
    {
        string result = base64Str.Trim();
        try
        {
            if (string.IsNullOrWhiteSpace(base64Str.Trim()))
            {
                return "";
            }

            base64Str = base64Str.Trim();
            string input = base64Str[randomPrefixStrLength..];

            input = randomPrefixStrLength == 0 ? input : RemoveBase64StrRandomStr(input);
            byte[] buffer = Convert.FromBase64String(input);
            result = encoding.GetString(buffer);
        }
        catch (Exception ex)
        {
            TextWriter output = Console.Out;
            lock (output)
            {
                var writer = new ConsoleWriter(output);
                try
                {
                    writer.BackgroundColor = ConsoleColor.DarkRed;
                    writer.ForegroundColor = ConsoleColor.Black;
                    writer.Write("fail");
                    writer.ResetColor();
                    writer.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                    writer.BackgroundColor = ConsoleColor.DarkRed;
                    writer.ForegroundColor = ConsoleColor.Black;
                    writer.WriteLine($"      Base64Util.Base64ToString: {ex}");
                }
                finally
                {
                    writer.ResetColor();
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 添加随机字符串到 Base64 字符串
    /// </summary>
    /// <param name="base64Str">Base64 字符串</param>
    /// <returns>添加随机字符串到 Base64 字符串</returns>
    private static string InsertRandomStrToBase64Str(string base64Str)
    {
        string strResult = $"{base64Str}";

        dic.Item.ForEach(item =>
        {
            if (item.Index < base64Str.Length)
            {
                char randomChar = base64Str[item.RandomIndex];
                strResult = strResult.Insert(item.Index, $"{randomChar}");
            }
        });

        return strResult;
    }

    /// <summary>
    /// 删除 Base64 字符串中的随机数
    /// </summary>
    /// <param name="input">Base64 字符串</param>
    /// <returns>删除 Base64 字符串中的随机数</returns>
    private static string RemoveBase64StrRandomStr(string input)
    {
        var items = dic.Item.OrderBy(x => x.Index).ToList();

        string strResult = $"{input}";

        items.ForEach(item =>
        {
            if (item.Index < strResult.Length)
            {
                strResult = strResult.Remove(item.Index, 1);
            }
        });

        return strResult;
    }

    private struct PwdDic
    {
        public List<PwdDicItem> Item { get; }

        public PwdDic(List<PwdDicItem> item)
        {
            Item = item;
        }
    }

    private readonly struct PwdDicItem
    {
        public int Index { get; }

        public int RandomIndex { get; }

        public PwdDicItem(int index, int randomIndex)
        {
            Index = index;
            RandomIndex = randomIndex;
        }
    }

    private static readonly PwdDic dic = new(new List<PwdDicItem>
    {
        new(950, 188),
        new(900, 201),
        new(800, 225),
        new(700, 255),
        new(600, 268),
        new(500, 277),
        new(400, 288),
        new(330, 327),
        new(300, 180),
        new(200, 178),
        new(100, 124),
        // 100 以内字典
        new(98, 95),
        new(92, 90),
        new(91, 87),
        new(88, 84),
        new(82, 79),
        new(78, 71),
        new(72, 69),
        new(68, 66),
        new(59, 55),
        new(48, 43),
        new(42, 37),
        new(36, 30),
        new(33, 27),
        new(24, 20),
        new(23, 18),
        new(21, 16),
        new(17, 14),
        new(13, 9),
        new(7, 4),
        new(5, 3),
        new(2, 1)
    });
}
