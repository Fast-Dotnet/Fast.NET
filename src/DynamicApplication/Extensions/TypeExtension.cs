// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.DynamicApplication;

/// <summary>
/// 为 <see cref="Type"/> 提供扩展方法
/// </summary>
internal static class TypeExtension
{
    /// <summary>
    /// 判断是否是富基元类型
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>类型可直接从路由、查询字符串等文本来源绑定时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsRichPrimitive(this Type type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.IsValueTuple())
        {
            return false;
        }

        // 数组需要按元素类型生成架构，不能仅按数组对象本身判断
        if (type.IsArray)
        {
            return type.GetElementType()?.IsRichPrimitive() == true;
        }

        // 基元、值类型和字符串可直接映射，无需展开成员
        if (type.IsPrimitive || type.IsValueType || type == typeof(string))
        {
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return type.GenericTypeArguments[0].IsRichPrimitive();
        }

        return false;
    }

    /// <summary>
    /// 判断是否是元组类型
    /// </summary>
    /// <param name="type">要检查的类型</param>
    /// <returns>类型为 <see cref="ValueTuple"/> 或其泛型形式时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsValueTuple(this Type type)
    {
        return type.Namespace == "System" && type.Name.Contains("ValueTuple`");
    }
}
