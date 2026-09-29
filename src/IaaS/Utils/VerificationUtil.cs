// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Security.Cryptography;
using System.Text;

namespace Fast.IaaS;

/// <summary>
/// 验证工具类
/// </summary>
public static class VerificationUtil
{
    /// <summary>
    /// 自定义进制所用的编码，大小写和数字(初始 62 位)，但去掉 7 位相似：O/o/0,I/i/1/l，去掉一个补位：A;最终只留(26+26+10)-(7+1)=54 位
    /// </summary>
    private static readonly char[] BASE =
    {
        '8', 'S', '2', 'H', 'b', 'V', 'c', 'E', 'Z', 'g', 'X', 'h', '9', 'z', 'y', 'C', 'x', '7', 'P', 'p', '5', 'K', 'B',
        'G', 'Q', 'U', 'F', 'R', '4', 'u', 'W', 'n', 'Y', 'D', 'd', 'e', 'f', 'a', '3', 't', 'M', 'q', 'J', 'r', 's', 'L',
        'm', 'T', 'N', 'w', '6', 'v', 'j', 'k'
    };

    /// <summary>
    /// A 补位字符，不能与自定义重复
    /// </summary>
    private static readonly char SUFFIX_CHAR = 'A';

    /// <summary>
    /// 进制长度
    /// </summary>
    private static readonly int BASE_LEN = BASE.Length;

    /// <summary>
    /// 生成邀请码最小长度
    /// </summary>
    private const int CODE_LEN = 6;

    /// <summary>
    /// 将Id转换为邀请码
    /// </summary>
    /// <param name="id">唯一标识</param>
    /// <returns>转换后的邀请码</returns>
    public static string IdToCodeByLong(long id)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Id 不能为负数。");
        }

        char[] buf = new char[BASE_LEN];
        int charPos = BASE_LEN;

        // Id超出字符表范围时取模，使结果稳定映射到有效索引
        while (id / BASE_LEN > 0)
        {
            int index = (int)(id % BASE_LEN);
            buf[--charPos] = BASE[index];
            id /= BASE_LEN;
        }

        buf[--charPos] = BASE[(int)(id % BASE_LEN)];
        // 将字符数组转化为字符串
        string result = new(buf, charPos, BASE_LEN - charPos);

        // 长度不足时使用随机字符补齐到目标长度
        int len = result.Length;
        if (len >= CODE_LEN)
        {
            return result;
        }

        var sb = new StringBuilder();
        sb.Append(SUFFIX_CHAR);
        // 扣除后缀占位符本身，计算仍需补齐的字符数
        for (int i = 0; i < CODE_LEN - len - 1; i++)
        {
            sb.Append(BASE[RandomNumberGenerator.GetInt32(BASE_LEN)]);
        }

        result += sb.ToString();

        return result;
    }

    /// <summary>
    /// 从邀请码解析Id
    /// </summary>
    /// <param name="code">业务或枚举编码</param>
    /// <returns>解析得到的Id</returns>
    public static long CodeToIdByLong(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("邀请码不能为空。", nameof(code));
        }

        char[] charArray = code.ToCharArray();
        long result = 0L;
        for (int i = 0; i < charArray.Length; i++)
        {
            if (charArray[i] == SUFFIX_CHAR)
            {
                break;
            }

            int index = Array.IndexOf(BASE, charArray[i]);
            if (index < 0)
            {
                throw new FormatException($"邀请码包含无效字符“{charArray[i]}”。");
            }

            result = checked(result * BASE_LEN + index);
        }

        return result;
    }

    /// <summary>
    /// 将Id转换为邀请码
    /// </summary>
    /// <param name="id">唯一标识</param>
    /// <returns>转换后的邀请码</returns>
    public static string IdToCodeByInt(int id)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Id 不能为负数。");
        }

        char[] buf = new char[BASE_LEN];
        int charPos = BASE_LEN;

        // Id超出字符表范围时取模，使结果稳定映射到有效索引
        while (id / BASE_LEN > 0)
        {
            int index = id % BASE_LEN;
            buf[--charPos] = BASE[index];
            id /= BASE_LEN;
        }

        buf[--charPos] = BASE[id % BASE_LEN];
        // 将字符数组转化为字符串
        string result = new(buf, charPos, BASE_LEN - charPos);

        // 长度不足时使用随机字符补齐到目标长度
        int len = result.Length;
        if (len >= CODE_LEN)
        {
            return result;
        }

        var sb = new StringBuilder();
        sb.Append(SUFFIX_CHAR);
        // 扣除后缀占位符本身，计算仍需补齐的字符数
        for (int i = 0; i < CODE_LEN - len - 1; i++)
        {
            sb.Append(BASE[RandomNumberGenerator.GetInt32(BASE_LEN)]);
        }

        result += sb.ToString();

        return result;
    }

    /// <summary>
    /// 从邀请码解析Id
    /// </summary>
    /// <param name="code">业务或枚举编码</param>
    /// <returns>解析得到的Id</returns>
    public static int CodeToIdByInt(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("邀请码不能为空。", nameof(code));
        }

        char[] charArray = code.ToCharArray();
        int result = 0;
        for (int i = 0; i < charArray.Length; i++)
        {
            if (charArray[i] == SUFFIX_CHAR)
            {
                break;
            }

            int index = Array.IndexOf(BASE, charArray[i]);
            if (index < 0)
            {
                throw new FormatException($"邀请码包含无效字符“{charArray[i]}”。");
            }

            result = checked(result * BASE_LEN + index);
        }

        return result;
    }

    /// <summary>
    /// 显示用于进制编码的所有字符串
    /// </summary>
    /// <returns>显示用于进制编码的所有字符串</returns>
    public static string PrintBase()
    {
        var upperCase = new StringBuilder();
        var lowerCase = new StringBuilder();
        var number = new StringBuilder();

        // 编码表的顺序参与Id编解码，绝不能为了展示而原地排序
        char[] sortedBase = (char[])BASE.Clone();
        Array.Sort(sortedBase);
        foreach (char item in sortedBase)
        {
            int ascii = item;
            if (ascii >= 48 && ascii <= 57)
            {
                number.Append(item);
            }
            else if (ascii >= 65 && ascii <= 90)
            {
                upperCase.Append(item);
            }
            else if (ascii >= 97 && ascii <= 122)
            {
                lowerCase.Append(item);
            }
        }

        string allStr = upperCase.Append(",").Append(lowerCase).Append(",").Append(number).ToString();
        return $"Count({allStr.Length - 2}):{allStr}";
    }

    /// <summary>
    /// 生成数字验证码
    /// </summary>
    /// <param name="len">目标长度</param>
    /// <returns>生成的数字验证码</returns>
    public static string GenNumVerCode(int len = CODE_LEN)
    {
        if (len <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(len), "验证码长度必须大于 0。");
        }

        var result = new StringBuilder(len);
        result.Append(RandomNumberGenerator.GetInt32(1, 10));
        for (int i = 1; i < len; i++)
        {
            result.Append(RandomNumberGenerator.GetInt32(10));
        }

        return result.ToString();
    }

    /// <summary>
    /// 生成字符串验证码
    /// </summary>
    /// <param name="len">目标长度</param>
    /// <returns>生成的字符串验证码</returns>
    public static string GenStrVerCode(int len = CODE_LEN)
    {
        if (len <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(len), "验证码长度必须大于 0。");
        }

        var result = new StringBuilder(len);

        for (int i = 0; i < len; i++)
        {
            int randomInt = RandomNumberGenerator.GetInt32(BASE_LEN);
            char randomChar = BASE[randomInt];
            result.Append(randomChar);
        }

        return result.ToString();
    }

    /// <summary>
    /// 生成随机数
    /// </summary>
    /// <param name="minVal">允许的最小值</param>
    /// <param name="maxVal">允许的最大值</param>
    /// <param name="isInclude">边界值是否包含在有效范围内</param>
    /// <returns>生成的随机数</returns>
    public static int GenRandomNum(int minVal, int maxVal, bool isInclude = false)
    {
        if (isInclude ? maxVal < minVal : maxVal <= minVal)
        {
            throw new ArgumentOutOfRangeException(nameof(maxVal), isInclude ? "最大值不能小于最小值。" : "最大值必须大于最小值。");
        }

        return GetRandomInt32(minVal, isInclude ? maxVal : maxVal - 1);
    }

    /// <summary>
    /// 生成包含上下边界的安全随机整数
    /// </summary>
    /// <param name="minValue">生成随机数时允许的最小值</param>
    /// <param name="maxValue">生成随机数时不包含的上限值</param>
    /// <returns>生成的包含上下边界的安全随机整数</returns>
    private static int GetRandomInt32(int minValue, int maxValue)
    {
        ulong range = (ulong)((long)maxValue - minValue + 1);
        const ulong sampleSpace = 1UL << 32;
        ulong limit = sampleSpace - sampleSpace % range;

        // 舍弃不能被区间长度整除的尾部样本，避免取模造成某些数字概率偏高
        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        uint sample;
        do
        {
            RandomNumberGenerator.Fill(bytes);
            sample = BitConverter.ToUInt32(bytes);
        } while (sample >= limit);

        return (int)(minValue + (long)(sample % range));
    }
}
