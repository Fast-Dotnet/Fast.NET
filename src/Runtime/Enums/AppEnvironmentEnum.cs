// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel;


// ReSharper disable once CheckNamespace
namespace System;

/// <summary>
/// App 运行环境枚举
/// </summary>
[Flags]
[FastEnum("App运行环境枚举")]
public enum AppEnvironmentEnum : long
{
    /// <summary>
    /// Web
    /// </summary>
    [Description("Web")]
    Web = 1L << 0,

    /// <summary>
    /// Windows
    /// </summary>
    [Description("Windows")]
    Windows = 1L << 1,

    /// <summary>
    /// Mac
    /// </summary>
    [Description("Mac")]
    Mac = 1L << 2,

    /// <summary>
    /// Linux
    /// </summary>
    [Description("Linux")]
    Linux = 1L << 3,

    /// <summary>
    /// Android
    /// </summary>
    [Description("Android")]
    Android = 1L << 5,

    /// <summary>
    /// 鸿蒙
    /// </summary>
    [Description("鸿蒙")]
    HarmonyOS = 1L << 6,

    /// <summary>
    /// IOS
    /// </summary>
    [Description("IOS")]
    IOS = 1L << 7,

    /// <summary>
    /// 快应用
    /// </summary>
    [Description("快应用")]
    QuickApp = 1L << 8,

    /// <summary>
    /// 微信小程序
    /// </summary>
    [Description("微信小程序")]
    WeChatMiniProgram = 1L << 9,

    /// <summary>
    /// 微信公众号
    /// </summary>
    [Description("微信公众号")]
    WeChatOfficialAccount = 1L << 10,

    /// <summary>
    /// 微信服务号
    /// </summary>
    [Description("微信服务号")]
    WeChatServiceAccount = 1L << 11,

    /// <summary>
    /// 微信开放平台
    /// </summary>
    [Description("微信开放平台")]
    WeChatOpenPlatform = 1L << 12,

    /// <summary>
    /// 企业微信
    /// </summary>
    [Description("企业微信")]
    WorkWeChat = 1L << 13,

    /// <summary>
    /// 支付宝小程序
    /// </summary>
    [Description("支付宝小程序")]
    AlipayMiniProgram = 1L << 14,

    /// <summary>
    /// 抖音小程序
    /// </summary>
    [Description("抖音小程序")]
    TiktokMiniProgram = 1L << 15,

    /// <summary>
    /// 钉钉小程序
    /// </summary>
    [Description("钉钉小程序")]
    DingTalkMiniProgram = 1L << 16,

    /// <summary>
    /// 飞书小程序
    /// </summary>
    [Description("飞书小程序")]
    FeiShuMiniProgram = 1L << 17,

    /// <summary>
    /// QQ 小程序
    /// </summary>
    [Description("QQ小程序")]
    QQMiniProgram = 1L << 18,

    /// <summary>
    /// 百度小程序
    /// </summary>
    [Description("百度小程序")]
    BaiduMiniProgram = 1L << 19,

    /// <summary>
    /// 快手小程序
    /// </summary>
    [Description("快手小程序")]
    KuaiShouMiniProgram = 1L << 20,

    /// <summary>
    /// 小红书小程序
    /// </summary>
    [Description("小红书小程序")]
    XiaoHongShuMiniProgram = 1L << 21,

    /// <summary>
    /// 京东小程序
    /// </summary>
    [Description("京东小程序")]
    JDMiniProgram = 1L << 22,

    /// <summary>
    /// API
    /// </summary>
    [Description("Api")]
    Api = 1L << 32,

    /// <summary>
    /// 其他
    /// </summary>
    [Description("其他")]
    Other = 1L << 33,

    /// <summary>
    /// 桌面端
    /// </summary>
    [Description("桌面端")]
    Desktop = Windows | Mac | Linux,

    /// <summary>
    /// 移动端
    /// </summary>
    [Description("移动端")]
    Mobile = Android | HarmonyOS | IOS,

    /// <summary>
    /// 小程序
    /// </summary>
    [Description("小程序")]
    MiniProgram = QuickApp
                  | WeChatMiniProgram
                  | AlipayMiniProgram
                  | TiktokMiniProgram
                  | DingTalkMiniProgram
                  | FeiShuMiniProgram
                  | QQMiniProgram
                  | BaiduMiniProgram
                  | KuaiShouMiniProgram
                  | XiaoHongShuMiniProgram
                  | JDMiniProgram,

    /// <summary>
    /// 移动端（三端）
    /// </summary>
    [Description("移动端（三端）")]
    MobileThree = Mobile | MiniProgram
}
