// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;

namespace Fast.IaaS;

/// <summary>
/// 坐标工具类
/// </summary>
/// <remarks>
/// <para>WGS-84：全球标准坐标系，常用于 GPS 定位</para>
/// <para>GCJ-02：中国国测局坐标系，又称火星坐标系，国内地图（高德、腾讯、百度等）使用</para>
/// </remarks>
public static class CoordinateUtil
{
    /// <summary>
    /// π 常量，用于角度与弧度转换
    /// </summary>
    private const double pi = 3.1415926535897932384626;

    /// <summary>
    /// 长半轴，地球椭球体的长半轴（单位：米）
    /// </summary>
    private const double a = 6378245.0;

    /// <summary>
    /// 偏心率平方，椭球体的形状参数
    /// </summary>
    private const double ee = 0.00669342162296594323;

    /// <summary>
    /// 判断给定经纬度是否在中国境内 <para>GCJ-02 偏移只在中国境内有效，境外坐标无需转换</para>
    /// </summary>
    /// <param name="lat">纬度</param>
    /// <param name="lng">经度</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsInChina(double lat, double lng)
    {
        if (lng < 72.004 || lng > 137.8347)
        {
            return false;
        }

        if (lat < 0.8293 || lat > 55.8271)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 纬度偏移计算公式
    /// </summary>
    /// <remarks>根据国测局算法对纬度进行偏移</remarks>
    /// <param name="x">参与坐标转换的横向分量</param>
    /// <param name="y">参与坐标转换的纵向分量</param>
    /// <returns>纬度偏移计算公式</returns>
    private static double TransformLat(double x, double y)
    {
        double ret = -100.0 + 2.0 * x + 3.0 * y + 0.2 * y * y + 0.1 * x * y + 0.2 * Math.Sqrt(Math.Abs(x));
        ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
        ret += (20.0 * Math.Sin(y * pi) + 40.0 * Math.Sin(y / 3.0 * pi)) * 2.0 / 3.0;
        ret += (160.0 * Math.Sin(y / 12.0 * pi) + 320 * Math.Sin(y * pi / 30.0)) * 2.0 / 3.0;
        return ret;
    }

    /// <summary>
    /// 经度偏移计算公式
    /// </summary>
    /// <remarks>根据国测局算法对经度进行偏移</remarks>
    /// <param name="x">参与坐标转换的横向分量</param>
    /// <param name="y">参与坐标转换的纵向分量</param>
    /// <returns>经度偏移计算公式</returns>
    private static double TransformLng(double x, double y)
    {
        double ret = 300.0 + x + 2.0 * y + 0.1 * x * x + 0.1 * x * y + 0.1 * Math.Sqrt(Math.Abs(x));
        ret += (20.0 * Math.Sin(6.0 * x * pi) + 20.0 * Math.Sin(2.0 * x * pi)) * 2.0 / 3.0;
        ret += (20.0 * Math.Sin(x * pi) + 40.0 * Math.Sin(x / 3.0 * pi)) * 2.0 / 3.0;
        ret += (150.0 * Math.Sin(x / 12.0 * pi) + 300.0 * Math.Sin(x / 30.0 * pi)) * 2.0 / 3.0;
        return ret;
    }

    /// <summary>
    /// WGS-84 -> GCJ-02（火星坐标）
    /// </summary>
    /// <remarks>如果坐标在中国境外，则返回原坐标</remarks>
    /// <param name="wgLat">纬度</param>
    /// <param name="wgLng">经度</param>
    /// <returns>WGS-84 -> GCJ-02（火星坐标）</returns>
    public static (double lat, double lng) WGS84ToGCJ02(double wgLat, double wgLng)
    {
        if (!IsInChina(wgLat, wgLng))
        {
            return (wgLat, wgLng);
        }

        // 偏移量计算
        double dLat = TransformLat(wgLng - 105.0, wgLat - 35.0);
        double dLng = TransformLng(wgLng - 105.0, wgLat - 35.0);

        // 纬度弧度化
        double radLat = wgLat / 180.0 * pi;

        // 椭球体修正系数
        double magic = Math.Sin(radLat);
        magic = 1 - ee * magic * magic;
        double sqrtMagic = Math.Sqrt(magic);

        // 调整偏移量为实际经纬度偏移
        dLat = dLat * 180.0 / (a * (1 - ee) / (magic * sqrtMagic) * pi);
        dLng = dLng * 180.0 / (a / sqrtMagic * Math.Cos(radLat) * pi);

        // 返回加上偏移后的 GCJ-02 坐标
        double mgLat = wgLat + dLat;
        double mgLng = wgLng + dLng;
        return (mgLat, mgLng);
    }

    /// <summary>
    /// GCJ-02（火星坐标）-> WGS-84
    /// </summary>
    /// <remarks>精确逆算比较复杂，这里使用迭代近似方法</remarks>
    /// <param name="mgLat">纬度</param>
    /// <param name="mgLng">经度</param>
    /// <returns>GCJ-02（火星坐标）-> WGS-84</returns>
    public static (double lat, double lng) GCJ02ToWGS84(double mgLat, double mgLng)
    {
        if (!IsInChina(mgLat, mgLng))
        {
            return (mgLat, mgLng);
        }

        // 先将 GCJ-02 坐标正向转换到 WGS-84 坐标
        (double lat1, double lng1) = WGS84ToGCJ02(mgLat, mgLng);

        // 偏移量
        double dLat = lat1 - mgLat;
        double dLng = lng1 - mgLng;

        // WGS-84 近似值 = GCJ-02 坐标 - 偏移量
        return (mgLat - dLat, mgLng - dLng);
    }
}
