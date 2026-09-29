// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI 文档组件声明 DTO
/// </summary>
public class OpenApiDocumentComponentSchemaDto
{
    /// <summary>
    /// 枚举
    /// </summary>
    public List<long> Enum { get; set; }

    /// <summary>
    /// 类型
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 格式
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// 可为空。
    /// </summary>
    public bool Nullable { get; set; }

    /// <summary>
    /// 声明项。
    /// </summary>
    public OpenApiDocumentSchemaPropertyDto Items { get; set; }

    /// <summary>
    /// 架构属性定义
    /// </summary>
    public IDictionary<string, OpenApiDocumentSchemaPropertyDto> Properties { get; set; }

    /// <summary>
    /// 必填属性名称。
    /// </summary>
    public HashSet<string> Required { get; set; }

    /// <summary>
    /// 附加属性
    /// </summary>
    [JsonIgnore]
    public bool AdditionalProperties
    {
        get => _additionalProperties;
        set
        {
            _additionalProperties = value;
            _additionalPropertiesSchema = JsonSerializer.SerializeToElement(value);
        }
    }

    /// <summary>
    /// 附加属性的原始定义。
    /// </summary>
    /// <remarks>用于同时兼容 OpenAPI 中的布尔值和架构对象。</remarks>
    [JsonPropertyName("additionalProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement AdditionalPropertiesSchema
    {
        get
        {
            if (_additionalPropertiesSchema.ValueKind != JsonValueKind.Undefined)
            {
                return _additionalPropertiesSchema;
            }

            return JsonSerializer.SerializeToElement(AdditionalProperties);
        }
        set
        {
            _additionalPropertiesSchema = value;
            _additionalProperties = value.ValueKind is JsonValueKind.True or JsonValueKind.Object;
        }
    }

    /// <summary>
    /// 引用。
    /// </summary>
    [JsonPropertyName("$ref")]
    public string Ref { get; set; }

    /// <summary>
    /// 全部匹配的组合架构。
    /// </summary>
    public List<OpenApiDocumentSchemaPropertyDto> AllOf { get; set; }

    /// <summary>
    /// 任一匹配的组合架构。
    /// </summary>
    public List<OpenApiDocumentSchemaPropertyDto> AnyOf { get; set; }

    /// <summary>
    /// 唯一匹配的组合架构。
    /// </summary>
    public List<OpenApiDocumentSchemaPropertyDto> OneOf { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }

    private bool _additionalProperties;

    private JsonElement _additionalPropertiesSchema;
}
