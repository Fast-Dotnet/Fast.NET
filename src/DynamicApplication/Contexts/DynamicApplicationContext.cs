// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

namespace Fast.DynamicApplication;

/// <summary>
/// Dynamic Application 上下文
/// </summary>
[SuppressSniffer]
public static class DynamicApplicationContext
{
    /// <summary>
    /// <see cref="IsApiController(Type)"/> 缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<Type, bool> IsApiControllerCached = new();

    /// <summary>
    /// 路由前缀
    /// </summary>
    public static string RoutePrefix { get; internal set; } = null;

    /// <summary>
    /// 控制器排序集合
    /// </summary>
    public static ConcurrentDictionary<string, (string, int, Type)> ControllerOrderCollection { get; set; } = new();

    /// <summary>
    /// 是否是 API 控制器
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsApiController(Type type)
    {
        return IsApiControllerCached.GetOrAdd(type, Function);

        // 本地静态方法
        static bool Function(Type type)
        {
            if (type == null)
            {
                return false;
            }

            // 排除 OData 控制器
            if (type.Assembly.GetName().Name?.StartsWith("Microsoft.AspNetCore.OData") == true)
            {
                return false;
            }

            // 不能是非公开，基元类型，值类型，抽象类，接口，泛型类
            if (!type.IsPublic
                || type.IsPrimitive
                || type.IsValueType
                || type.IsAbstract
                || type.IsInterface
                || type.IsGenericType)
            {
                return false;
            }

            // 继承 ControllerBase 或 实现 IApplication 的类型
            if ((!typeof(Controller).IsAssignableFrom(type) && typeof(ControllerBase).IsAssignableFrom(type))
                || typeof(IDynamicApplication).IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }
    }
}
