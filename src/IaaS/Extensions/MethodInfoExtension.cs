// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="MethodInfo"/> 提供扩展方法
/// </summary>
public static class MethodInfoExtension
{
    /// <summary>
    /// 判断方法是否是异步
    /// </summary>
    /// <param name="methodInfo">目标方法的反射元数据</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsAsync(this MethodInfo methodInfo)
    {
        Type returnType = methodInfo.ReturnType;
        return methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>() != null
               || typeof(Task).IsAssignableFrom(returnType)
               || returnType == typeof(ValueTask)
               || (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>));
    }

    /// <summary>
    /// 获取方法真实返回类型
    /// </summary>
    /// <param name="methodInfo">目标方法的反射元数据</param>
    /// <returns>获取到的方法真实返回类型</returns>
    public static Type GetRealReturnType(this MethodInfo methodInfo)
    {
        bool isAsyncMethod = methodInfo.IsAsync();

        Type returnType = methodInfo.ReturnType;
        return isAsyncMethod ? returnType.GenericTypeArguments.FirstOrDefault() ?? typeof(void) : returnType;
    }

    /// <summary>
    /// 查找方法指定特性，如果没找到则继续查找声明类
    /// </summary>
    /// <param name="methodInfo">目标方法的反射元数据</param>
    /// <param name="inherit">是否在基类型中继续查找成员或特性</param>
    /// <typeparam name="TAttribute">要查找的特性类型</typeparam>
    /// <returns>查找方法指定特性，如果没找到则继续查找声明类</returns>
    public static TAttribute GetFoundAttribute<TAttribute>(this MethodInfo methodInfo, bool inherit) where TAttribute : Attribute
    {
        Type declaringType = methodInfo.DeclaringType;

        Type attributeType = typeof(TAttribute);

        if (methodInfo.IsDefined(attributeType, inherit))
        {
            return methodInfo.GetCustomAttribute<TAttribute>(inherit);
        }

        // 方法未声明特性时，继续检查其声明类型
        if (declaringType == null)
        {
            return null;
        }

        if (declaringType.IsDefined(attributeType, inherit))
        {
            return declaringType.GetCustomAttribute<TAttribute>(inherit);
        }

        return null;
    }

    /// <summary>
    /// 查找方法指定特性，如果没找到则继续查找声明类
    /// </summary>
    /// <param name="methodInfo">目标方法的反射元数据</param>
    /// <param name="attributeType">要读取的特性类型</param>
    /// <param name="inherit">是否在基类型中继续查找成员或特性</param>
    /// <returns>查找方法指定特性，如果没找到则继续查找声明类</returns>
    public static Attribute GetFoundAttribute(this MethodInfo methodInfo, Type attributeType, bool inherit)
    {
        Type declaringType = methodInfo.DeclaringType;

        if (methodInfo.IsDefined(attributeType, inherit))
        {
            return methodInfo.GetCustomAttribute(attributeType, inherit);
        }

        // 方法未声明特性时，继续检查其声明类型
        if (declaringType == null)
        {
            return null;
        }

        if (declaringType.IsDefined(attributeType, inherit))
        {
            return declaringType.GetCustomAttribute(attributeType, inherit);
        }

        return null;
    }

    /// <summary>
    /// 获取方法参数数量
    /// </summary>
    /// <param name="methodInfo">目标方法的反射元数据</param>
    /// <returns>满足条件的项数</returns>
    public static int GetMethodParameterCount(this MethodInfo methodInfo)
    {
        return methodInfo.GetParameters().Length;
    }
}
