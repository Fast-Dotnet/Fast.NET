// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using System.Reflection.Emit;

namespace Fast.Runtime;

/// <summary>
/// 为 <see cref="Type"/> 提供扩展方法
/// </summary>
public static class TypeExtension
{
    /// <summary>
    /// 创建属性值设置器
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="propertyInfo">目标属性的反射元数据</param>
    /// <returns>创建的属性值设置器</returns>
    public static Action<object, object> CreatePropertySetter(this Type type, PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        // 创建一个新的动态方法，并为其命名，命名格式为类型全名_设置_属性名
        var setterMethod = new DynamicMethod($"{type.FullName}_Set_{propertyInfo.Name}", null,
            new[] {typeof(object), typeof(object)}, typeof(TypeExtensions).Module);

        // 获取动态方法的 IL 生成器
        ILGenerator ilGenerator = setterMethod.GetILGenerator();

        // 获取属性的设置方法，并允许非公开访问
        MethodInfo setMethod = propertyInfo.GetSetMethod(true);

        ArgumentNullException.ThrowIfNull(setMethod);

        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Castclass, type);

        ilGenerator.Emit(OpCodes.Ldarg_1);

        // 检查属性类型是否为值类型
        if (propertyInfo.PropertyType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);
        }
        else
        {
            // 将值转换为属性类型
            ilGenerator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
        }

        // 在目标对象上调用设置方法
        ilGenerator.Emit(OpCodes.Callvirt, setMethod);

        // 从动态方法返回
        ilGenerator.Emit(OpCodes.Ret);

        return (Action<object, object>)setterMethod.CreateDelegate(typeof(Action<object, object>));
    }

    /// <summary>
    /// 创建字段值设置器
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="fieldInfo">目标字段的反射元数据</param>
    /// <returns>创建的字段值设置器</returns>
    public static Action<object, object> CreateFieldSetter(this Type type, FieldInfo fieldInfo)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);

        // 创建一个新的动态方法，并为其命名，命名格式为类型全名_设置_字段名
        var setterMethod = new DynamicMethod($"{type.FullName}_Set_{fieldInfo.Name}", null,
            new[] {typeof(object), typeof(object)}, typeof(TypeExtensions).Module);

        // 获取动态方法的 IL 生成器
        ILGenerator ilGenerator = setterMethod.GetILGenerator();

        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Castclass, type);

        ilGenerator.Emit(OpCodes.Ldarg_1);

        // 检查字段类型是否为值类型
        if (fieldInfo.FieldType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
        }
        else
        {
            // 将值转换为字段类型
            ilGenerator.Emit(OpCodes.Castclass, fieldInfo.FieldType);
        }

        // 将堆栈上的值存储到字段中
        ilGenerator.Emit(OpCodes.Stfld, fieldInfo);

        // 从动态方法返回
        ilGenerator.Emit(OpCodes.Ret);

        return (Action<object, object>)setterMethod.CreateDelegate(typeof(Action<object, object>));
    }

    /// <summary>
    /// 创建属性值访问器
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="propertyInfo">目标属性的反射元数据</param>
    /// <returns>创建的属性值访问器</returns>
    public static Func<object, object> CreatePropertyGetter(this Type type, PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        ArgumentNullException.ThrowIfNull(propertyInfo.DeclaringType);

        // 创建一个新的动态方法，并为其命名，命名格式为类型全名_获取_属性名
        var dynamicMethod = new DynamicMethod($"{type.FullName}_Get_{propertyInfo.Name}", typeof(object), new[] {typeof(object)},
            typeof(TypeExtensions).Module, true);

        // 获取动态方法的 IL 生成器
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

        // 获取属性的获取方法，并允许非公开访问
        MethodInfo getMethod = propertyInfo.GetGetMethod(true);

        ArgumentNullException.ThrowIfNull(getMethod);

        // 将目标对象加载到堆栈上，并将其转换为声明类型
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);

        // 调用获取方法
        ilGenerator.EmitCall(OpCodes.Callvirt, getMethod, null);

        // 如果属性类型为值类型，则装箱为 object 类型
        if (propertyInfo.PropertyType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, propertyInfo.PropertyType);
        }

        // 从动态方法返回
        ilGenerator.Emit(OpCodes.Ret);

        return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
    }

    /// <summary>
    /// 创建字段值访问器
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <param name="fieldInfo">目标字段的反射元数据</param>
    /// <returns>创建的字段值访问器</returns>
    public static Func<object, object> CreateFieldGetter(this Type type, FieldInfo fieldInfo)
    {
        ArgumentNullException.ThrowIfNull(fieldInfo);
        ArgumentNullException.ThrowIfNull(fieldInfo.DeclaringType);

        // 创建一个新的动态方法，并为其命名，命名格式为类型全名_获取_字段名
        var dynamicMethod = new DynamicMethod($"{type.FullName}_Get_{fieldInfo.Name}", typeof(object), new[] {typeof(object)},
            typeof(TypeExtensions).Module, true);

        // 获取动态方法的 IL 生成器
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();

        // 将目标对象加载到堆栈上，并将其转换为字段的声明类型
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Castclass, fieldInfo.DeclaringType);

        // 加载字段的值到堆栈上
        ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);

        // 如果字段类型为值类型，则装箱为 object 类型
        if (fieldInfo.FieldType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, fieldInfo.FieldType);
        }

        // 从动态方法返回
        ilGenerator.Emit(OpCodes.Ret);

        return (Func<object, object>)dynamicMethod.CreateDelegate(typeof(Func<object, object>));
    }
}
