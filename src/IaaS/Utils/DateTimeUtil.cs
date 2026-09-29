// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;

namespace Fast.IaaS;

/// <summary>
/// DateTime 工具类
/// </summary>
public static class DateTimeUtil
{
    /// <summary>
    /// 获取指定年月的第一天
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>DateTime 第一天的 DateTime</returns>
    public static DateTime GetYearMonthFirstDay(string year, string month)
    {
        // 以指定月份第一天的零点作为时间范围起点
        var internalDate = Convert.ToDateTime($"{year}-{month}-01 00:00:00");
        return internalDate;
    }

    /// <summary>
    /// 获取指定年月的第一天
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>DateTime 第一天的 DateTime</returns>
    public static DateTime GetYearMonthFirstDay(int year, int month)
    {
        // 以指定月份第一天的零点作为时间范围起点
        var internalDate = new DateTime(year, month, 01, 00, 00, 00);
        return internalDate;
    }

    /// <summary>
    /// 获取指定年月的最后一天
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>DateTime 最后一天的 DateTime</returns>
    public static DateTime GetYearMonthLastDay(string year, string month)
    {
        // 以指定月份第一天的结束时刻作为时间范围终点
        var internalDate = Convert.ToDateTime($"{year}-{month}-01 23:59:59");
        return internalDate.AddMonths(+1).AddDays(-1);
    }

    /// <summary>
    /// 获取指定年月的最后一天
    /// </summary>
    /// <param name="year">年份</param>
    /// <param name="month">月份</param>
    /// <returns>DateTime 最后一天的 DateTime</returns>
    public static DateTime GetYearMonthLastDay(int year, int month)
    {
        // 以指定月份第一天的结束时刻作为时间范围终点
        var internalDate = new DateTime(year, month, 01, 23, 59, 59);
        return internalDate.AddMonths(+1).AddDays(-1);
    }

    /// <summary>
    /// 计算两个时间的差，返回天数
    /// </summary>
    /// <param name="startTime">时间范围的开始时间</param>
    /// <param name="lastTime">上一次执行或记录的时间</param>
    /// <returns>计算得到的两个时间的差，返回天数</returns>
    public static int DateDiffDay(DateTime startTime, DateTime lastTime)
    {
        var start = Convert.ToDateTime(startTime.ToShortDateString());
        var end = Convert.ToDateTime(lastTime.ToShortDateString());
        TimeSpan sp = end.Subtract(start);
        return sp.Days;
    }
}
