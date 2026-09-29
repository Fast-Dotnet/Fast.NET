// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fast.Swagger;

/// <summary>
/// 修正 规范化文档 Enum 提示
/// </summary>
internal sealed class EnumSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// 中文正则表达式
    /// </summary>
    private const string CHINESE_PATTERN = @"[\u4e00-\u9fa5]";

    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        Type type = context.Type;

        // 排除其他程序集的枚举
        if (type.IsEnum && MAppContext.Assemblies.Contains(type.Assembly) && schema is OpenApiSchema model)
        {
            model.Enum?.Clear();

            Array enumValues = Enum.GetValues(type);

            // 从配置文件中读取全局配置
            bool convertToNumber = Penetrates.SwaggerSettings.EnumToNumber!.Value;

            // 包含中文情况
            if (Enum.GetNames(type).Any(v => Regex.IsMatch(v, CHINESE_PATTERN)))
            {
                convertToNumber = true;
            }

            // 获取枚举实际值类型
            Type enumValueType = type.GetField("value__")?.FieldType;

            foreach (object value in enumValues)
            {
                object numValue = value.ChangeType(enumValueType);

                // Microsoft.OpenAPI 2.x 使用 JsonNode 表示枚举值
                model.Enum?.Add(!convertToNumber ? JsonValue.Create(value.ToString()) : JsonNode.Parse($"{numValue}"));
            }

            if (!convertToNumber)
            {
                model.Type = JsonSchemaType.String;
                model.Format = null;
            }
        }
    }
}
