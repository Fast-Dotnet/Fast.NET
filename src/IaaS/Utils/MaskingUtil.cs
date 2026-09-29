// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Linq;

namespace Fast.IaaS;

/// <summary>
/// 数据脱敏工具类
/// </summary>
public static class MaskingUtil
{
    /// <summary>
    /// 姓名脱敏（只保留首字）
    /// </summary>
    /// <param name="name">名称</param>
    /// <returns>姓名脱敏（只保留首字）</returns>
    public static string NameMasking(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length == 1)
        {
            return name;
        }

        if (name.Length == 2)
        {
            return $"{name[0]}*";
        }

        return name[0] + new string('*', name.Length - 1);
    }

    /// <summary>
    /// 姓名脱敏（保留首尾）
    /// </summary>
    /// <param name="name">名称</param>
    /// <returns>姓名脱敏（保留首尾）</returns>
    public static string NameKeepLastMasking(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length == 1)
        {
            return name;
        }

        if (name.Length == 2)
        {
            return $"{name[0]}*";
        }

        return name[0] + new string('*', name.Length - 2) + name[^1];
    }

    /// <summary>
    /// 手机号脱敏（152****5552）
    /// </summary>
    /// <param name="mobile">要脱敏的手机号码</param>
    /// <returns>手机号脱敏（152****5552）</returns>
    public static string MobileMasking(string mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile) || mobile.Length < 7)
        {
            return mobile;
        }

        return mobile.Substring(0, 3) + "****" + mobile[^4..];
    }

    /// <summary>
    /// 身份证脱敏处理（前 4 后 4）
    /// </summary>
    /// <param name="idCard">要脱敏的身份证号码</param>
    /// <returns>身份证脱敏处理（前 4 后 4）</returns>
    public static string IdCardMasking(string idCard)
    {
        if (string.IsNullOrWhiteSpace(idCard) || idCard.Length < 8)
        {
            return idCard;
        }

        return idCard[..4] + new string('*', idCard.Length - 8) + idCard[^4..];
    }

    /// <summary>
    /// 邮箱脱敏（最多保留 3 位字符 + 域名）
    /// </summary>
    /// <param name="email">要脱敏的电子邮箱地址</param>
    /// <returns>邮箱脱敏（最多保留 3 位字符 + 域名）</returns>
    public static string EmailMasking(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        int index = email.IndexOf('@');
        if (index <= 0)
        {
            return email;
        }

        string user = email[..index];
        string domain = email[index..];

        return user.Length switch
        {
            1 => email,
            2 => user[0] + "*" + domain,
            3 => user[..2] + "*" + domain,
            _ => user[..3] + new string('*', user.Length - 3) + domain
        };
    }

    /// <summary>
    /// 银行卡脱敏（前 6 后 4）
    /// </summary>
    /// <param name="cardNo">要脱敏的银行卡号</param>
    /// <returns>银行卡脱敏（前 6 后 4）</returns>
    public static string BankCardMasking(string cardNo)
    {
        if (string.IsNullOrWhiteSpace(cardNo) || cardNo.Length < 10)
        {
            return cardNo;
        }

        return cardNo[..6] + new string('*', cardNo.Length - 10) + cardNo[^4..];
    }

    /// <summary>
    /// 地址脱敏（优先识别省/市/区/街道等行政区划，保留上级区域）
    /// </summary>
    /// <param name="address">目标服务地址</param>
    /// <returns>地址脱敏（优先识别省/市/区/街道等行政区划，保留上级区域）</returns>
    public static string AddressMasking(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return address;
        }

        string[] keys = ["省", "市", "区", "县", "乡", "镇", "街道", "社区"];

        foreach (string key in keys)
        {
            int index = address.IndexOf(key, StringComparison.Ordinal);
            if (index > 0 && index + 1 < address.Length)
            {
                return address[..(index + 1)] + "****";
            }
        }

        // 无法识别地址格式时保留前 6 个字符
        if (address.Length <= 6)
        {
            return address;
        }

        return address[..6] + "****";
    }

    /// <summary>
    /// 车牌号脱敏（保留前两位，如有分隔符则保留“省份+地区字母+分隔符”）
    /// </summary>
    /// <param name="carNumber">要验证的车牌号码</param>
    /// <returns>车牌号脱敏（保留前两位，如有分隔符则保留“省份+地区字母+分隔符”）</returns>
    public static string CarNumberMasking(string carNumber)
    {
        if (string.IsNullOrWhiteSpace(carNumber) || carNumber.Length <= 2)
        {
            return carNumber;
        }

        // 支持车牌文本中常见的地区分隔符
        char[] separators = ['·', '•', '.', '-', ' '];

        // 第三个字符是分隔符时保留前两位及分隔符，其余字符全部脱敏
        if (carNumber.Length >= 3 && separators.Contains(carNumber[2]))
        {
            string head = carNumber[..3];
            int tailLen = carNumber.Length - 3;
            return head + new string('*', tailLen);
        }

        // 普通格式仅保留前两位，其余字符按原长度脱敏
        return carNumber[..2] + new string('*', carNumber.Length - 2);
    }

    /// <summary>
    /// IP 地址脱敏（保留前两段）
    /// </summary>
    /// <param name="ip">要验证的 IP 地址</param>
    /// <returns>IP 地址脱敏（保留前两段）</returns>
    public static string IpMasking(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return ip;
        }

        string[] parts = ip.Split('.');
        if (parts.Length != 4)
        {
            return ip;
        }

        return $"{parts[0]}.{parts[1]}.*.*";
    }
}
