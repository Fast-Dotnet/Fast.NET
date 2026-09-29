// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.Runtime;

/// <summary>
/// 公网 IP 信息
/// </summary>
[SuppressSniffer]
public class WanNetIPInfo
{
    /// <summary>
    /// Ip 地址
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// 省份
    /// </summary>
    public string Province { get; set; }

    /// <summary>
    /// 省份邮政编码
    /// </summary>
    public string ProvinceZipCode { get; set; }

    /// <summary>
    /// 城市
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// 城市邮政编码
    /// </summary>
    public string CityZipCode { get; set; }

    /// <summary>
    /// 地理信息
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// 运营商
    /// </summary>
    public string Operator => Address[(Province.Length + City.Length)..].Trim();
}
