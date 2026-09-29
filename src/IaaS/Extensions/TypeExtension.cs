// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="Type"/> 提供扩展方法
/// </summary>
public static class TypeExtension
{
    /// <summary>
    /// 判断类型是否实现某个泛型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="generic">是否按泛型类型规则进行匹配</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
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

    /// <summary>
    /// 获取类型所在程序集名称
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>获取到的类型所在程序集名称</returns>
    public static string GetAssemblyName(this Type type)
    {
        return type.GetTypeInfo().GetAssemblyName();
    }

    /// <summary>
    /// 获取类型所在程序集名称
    /// </summary>
    /// <param name="typeInfo">目标类型的反射元数据</param>
    /// <returns>获取到的类型所在程序集名称</returns>
    public static string GetAssemblyName(this TypeInfo typeInfo)
    {
        return typeInfo.Assembly.GetAssemblyName();
    }

    /// <summary>
    /// 判断是否是富基元类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsRichPrimitive(this Type type)
    {
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
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsValueTuple(this Type type)
    {
        return type.Namespace == "System" && type.Name.Contains("ValueTuple`");
    }

    /// <summary>
    /// 检查类型是否是静态类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsStatic(this Type type)
    {
        return type is {IsSealed: true, IsAbstract: true};
    }

    /// <summary>
    /// 检查类型是否是匿名类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsAnonymous(this Type type)
    {
        // 检查是否贴有 [CompilerGenerated] 特性
        if (!type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return false;
        }

        // 类型限定名是否以 <> 开头且以 AnonymousType 结尾
        return !(type.FullName is null) && type.FullName.StartsWith("<>") && type.FullName.Contains("AnonymousType");
    }

    /// <summary>
    /// 检查类型是否可实例化
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsInstantiable(this Type type)
    {
        return type is {IsClass: true, IsAbstract: false} && !type.IsStatic();
    }

    /// <summary>
    /// 检查类型是否派生自指定类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="fromType">from 类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsAlienAssignableTo(this Type type, Type fromType)
    {
        if (fromType is null)
        {
            throw new ArgumentNullException(nameof(fromType));
        }

        return fromType != type && fromType.IsAssignableFrom(type);
    }

    /// <summary>
    /// 获取指定特性实例
    /// </summary>
    /// <remarks>若特性不存在则返回 <see langword="null"/></remarks>
    /// <param name="type">目标类型</param>
    /// <param name="inherit">是否在基类型中继续查找成员或特性</param>
    /// <typeparam name="TAttribute">要查找的特性类型</typeparam>
    /// <returns>获取到的指定特性实例</returns>
    public static TAttribute GetDefinedCustomAttribute<TAttribute>(this Type type, bool inherit = false)
        where TAttribute : Attribute
    {
        // 检查是否定义
        return !type.IsDefined(typeof(TAttribute), inherit) ? null : type.GetCustomAttribute<TAttribute>(inherit);
    }

    /// <summary>
    /// 检查类型是否定义了公开无参构造函数
    /// </summary>
    /// <remarks>用于 <see cref="Activator.CreateInstance(Type)"/> 实例化</remarks>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool HasDefinePublicParameterlessConstructor(this Type type)
    {
        return type.IsInstantiable()
               && type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null) != null;
    }

    /// <summary>
    /// 检查类型和指定类型定义是否相等
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="compareType">compare 类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsDefinitionEqual(this Type type, Type compareType)
    {
        if (compareType is null)
        {
            throw new ArgumentNullException(nameof(compareType));
        }

        return type == compareType
               || (type.IsGenericType
                   && compareType.IsGenericType
                   && type.IsGenericTypeDefinition // 💡
                   && type == compareType.GetGenericTypeDefinition());
    }

    /// <summary>
    /// 检查类型和指定继承类型是否兼容
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="inheritType">inherit 类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsCompatibilityTo(this Type type, Type inheritType)
    {
        if (inheritType is null)
        {
            throw new ArgumentNullException(nameof(inheritType));
        }

        return inheritType != typeof(object)
               && inheritType.IsAssignableFrom(type)
               && (!type.IsGenericType
                   || (type.IsGenericType
                       && inheritType.IsGenericType
                       && type.GetTypeInfo().GenericTypeParameters.SequenceEqual(inheritType.GenericTypeArguments)));
    }

    /// <summary>
    /// 检查类型是否定义了指定方法
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="name">方法名称</param>
    /// <param name="accessibilityBindingFlags">用于筛选成员可见性的绑定标志</param>
    /// <param name="methodInfo">目标方法的反射元数据</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsDeclarationMethod(this Type type, string name, BindingFlags accessibilityBindingFlags,
        out MethodInfo methodInfo)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"Argument '{name}' cannot be null or whitespace.");
        }

        methodInfo = type.GetMethod(name, accessibilityBindingFlags | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        return methodInfo != null;
    }

    /// <summary>
    /// 检查类型是否是整数类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsInteger(this Type type)
    {
        // 如果是枚举或浮点类型则直接返回
        if (type.IsEnum || type.IsDecimal())
        {
            return false;
        }

        TypeCode typeCode = Type.GetTypeCode(type);
        return typeCode == TypeCode.Byte
               || typeCode == TypeCode.SByte
               || typeCode == TypeCode.Int16
               || typeCode == TypeCode.Int32
               || typeCode == TypeCode.Int64
               || typeCode == TypeCode.UInt16
               || typeCode == TypeCode.UInt32
               || typeCode == TypeCode.UInt64;
    }

    /// <summary>
    /// 检查类型是否是小数类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsDecimal(this Type type)
    {
        // 如果是浮点类型则直接返回
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return true;
        }

        TypeCode typeCode = Type.GetTypeCode(type);
        return typeCode == TypeCode.Double || typeCode == TypeCode.Decimal;
    }

    /// <summary>
    /// 检查类型是否是数值类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsNumeric(this Type type)
    {
        return type.IsInteger() || type.IsDecimal();
    }

    /// <summary>
    /// 检查类型是否是字典类型
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsDictionary(this Type type)
    {
        // 如果是 IDictionary<,> 类型则直接返回
        if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
        {
            return true;
        }

        // 处理 KeyValuePair<,> 集合类型
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            // 检查是否是 KeyValuePair<,> 数组类型
            if (type.IsArray)
            {
                // 获取数组元素类型
                Type elementType = type.GetElementType();

                // 检查元素类型是否是 KeyValuePair<,> 类型
                if (elementType != null
                    && elementType.IsGenericType
                    && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return true;
                }
            }
            // 检查是否是 KeyValuePair<,> 集合类型
            else
            {
                // 检查集合项类型是否是 KeyValuePair<,> 类型
                if (type.IsGenericType
                    && type.GenericTypeArguments.Length == 1
                    && type.GenericTypeArguments[0].IsGenericType
                    && type.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 获取类型自定义特性
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="inherit">是否在基类型中继续查找成员或特性</param>
    /// <typeparam name="TAttribute">要查找的特性类型</typeparam>
    /// <returns>获取到的类型自定义特性</returns>
    public static TAttribute GetTypeAttribute<TAttribute>(this Type type, bool inherit = false) where TAttribute : Attribute
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        // 检查特性并获取特性对象
        return type.IsDefined(typeof(TAttribute), inherit) ? type.GetCustomAttribute<TAttribute>(inherit) : null;
    }
}
