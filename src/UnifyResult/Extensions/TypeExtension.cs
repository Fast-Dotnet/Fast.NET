// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.UnifyResult;

/// <summary>
/// 为 <see cref="Type"/> 提供扩展方法
/// </summary>
internal static class TypeExtension
{
    /// <summary>
    /// 判断类型是否实现某个泛型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="generic">泛型类型</param>
    /// <returns>目标类型自身、其基类型或接口匹配指定开放泛型时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool HasImplementedRawGeneric(this Type type, Type generic)
    {
        Type localType = type;
        bool isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
        if (isTheRawGenericType)
        {
            return true;
        }

        while (localType != null && localType != typeof(object))
        {
            isTheRawGenericType = IsTheRawGenericType(localType);
            if (isTheRawGenericType)
            {
                return true;
            }

            localType = localType.BaseType;
        }

        return false;

        bool IsTheRawGenericType(Type t)
        {
            return generic == (t.IsGenericType ? t.GetGenericTypeDefinition() : t);
        }
    }
}
