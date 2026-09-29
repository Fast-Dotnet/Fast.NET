// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Newtonsoft.Json;

namespace Fast.Serialization;

/// <summary>
/// Exception 类型 JSON 返回处理
/// </summary>
/// <remarks>解决 <see cref="Exception"/> 类型不能被正常序列化和反序列化操作</remarks>
internal sealed class ExceptionJsonConverter : JsonConverter<Exception>
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, Exception value, JsonSerializer serializer)
    {
        // 默认仅输出 Message、Source、StackTrace 和 InnerException
        string[] writeNameArr = new[]
        {
            nameof(Exception.Message), nameof(Exception.Source), nameof(Exception.StackTrace),
            nameof(Exception.InnerException)
        };
        // TargetSite 含有不可安全序列化的反射信息，因此从输出属性中排除
        var serializableProperties = value.GetType()
            .GetProperties()
            .Select(sl => new {sl.Name, Value = sl.GetValue(value)})
            .Where(wh => writeNameArr.Contains(wh.Name))
            .Where(wh => wh.Value != null)
            .ToList();

        if (serializableProperties.Count == 0)
        {
            return;
        }

        writer.WriteStartObject();

        foreach (var prop in serializableProperties)
        {
            writer.WritePropertyName(prop.Name);
            // 使用 JsonConvert.SerializeObject 来序列化属性值，确保处理属性值的类型
            serializer.Serialize(writer, prop.Value);
        }

        writer.WriteEndObject();
    }

    /// <inheritdoc />
    public override Exception ReadJson(JsonReader reader, Type objectType, Exception existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        // 异常对象只允许序列化输出，不支持从外部数据重建
        throw new NotSupportedException("Deserializing exceptions is not allowed.");
    }
}
