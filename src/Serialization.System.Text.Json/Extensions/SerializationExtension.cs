// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json;

namespace Fast.Serialization;

/// <summary>
/// 提供 System.Text.Json 序列化扩展方法
/// </summary>
public static class SerializationExtension
{
    /// <summary>
    /// 将 JSON 文本反序列化为对象
    /// </summary>
    /// <param name="json">要解析的 JSON 文本</param>
    /// <typeparam name="T">序列化或转换后的对象类型</typeparam>
    /// <returns>转换得到的对象</returns>
    public static T ToObject<T>(this string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json = json.Replace("&nbsp;", "", StringComparison.Ordinal);
        return JsonSerializer.Deserialize<T>(json, JsonContext.SerializerOptions);
    }

    /// <summary>
    /// 将 JSON 文本反序列化为对象
    /// </summary>
    /// <param name="json">要解析的 JSON 文本</param>
    /// <param name="type">目标类型</param>
    /// <returns>转换得到的对象</returns>
    public static object ToObject(this string json, Type type)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(type);
        json = json.Replace("&nbsp;", "", StringComparison.Ordinal);
        return JsonSerializer.Deserialize(json, type, JsonContext.SerializerOptions);
    }

    /// <summary>
    /// 将对象序列化为 JSON 文本
    /// </summary>
    /// <param name="obj">要处理的对象</param>
    /// <returns>序列化后的 JSON 文本</returns>
    public static string ToJsonString(this object obj)
    {
        return JsonSerializer.Serialize(obj, JsonContext.SerializerOptions);
    }

    /// <summary>
    /// 将字典数据转换为对象
    /// </summary>
    /// <param name="dictionary">要处理的字典</param>
    /// <typeparam name="T">序列化或转换后的对象类型</typeparam>
    /// <returns>转换得到的对象</returns>
    public static T ToObject<T>(this IDictionary<string, object> dictionary)
    {
        return dictionary.ToJsonString().ToObject<T>();
    }

    /// <summary>
    /// 将字典数据转换为对象
    /// </summary>
    /// <param name="dictionary">要处理的字典</param>
    /// <param name="type">目标类型</param>
    /// <returns>转换得到的对象</returns>
    public static object ToObject(this IDictionary<string, object> dictionary, Type type)
    {
        return dictionary.ToJsonString().ToObject(type);
    }

    /// <summary>
    /// 通过 JSON 序列化创建对象的深层副本
    /// </summary>
    /// <remarks>该方法通过一次序列化和反序列化创建副本，开销高于成员复制，且只保留可序列化的数据</remarks>
    /// <param name="source">需要拷贝的对象</param>
    /// <typeparam name="T">要复制的对象类型</typeparam>
    /// <returns>创建的对象副本</returns>
    public static T DeepCopy<T>(this T source)
    {
        return source is null ? default : source.ToJsonString().ToObject<T>();
    }
}
