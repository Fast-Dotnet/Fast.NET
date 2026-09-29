// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text;
using System.Text.Json;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI DTO 工具类
/// </summary>
public static partial class OpenApiUtil
{
    /// <summary>
    /// 处理声明引用 key
    /// </summary>
    /// <param name="refKey">OpenAPI 架构引用键</param>
    /// <param name="refSchemas">用于解析引用的 OpenAPI 架构集合</param>
    /// <returns>处理声明引用 key</returns>
    internal static string DisposeSchemaRefKey(string refKey, HashSet<string> refSchemas = null)
    {
        // 获取 $ref 最后一个/后的 Name
        refKey = refKey?.Split("/").LastOrDefault();

        return DisposeSchemaRefName(refKey, refSchemas);
    }

    /// <summary>
    /// 处理基础类型
    /// </summary>
    /// <param name="refKey">OpenAPI 架构引用键</param>
    /// <returns>处理基础类型</returns>
    internal static string DisposeBaseType(string refKey)
    {
        return FindBaseTypeMapping(refKey) ?? "unknown";
    }

    /// <summary>
    /// 处理 OpenAPI 架构类型。
    /// </summary>
    /// <param name="schema">OpenAPI 架构。</param>
    /// <param name="refSchemas">用于解析引用的 OpenAPI 架构集合。</param>
    /// <returns>TypeScript 类型；架构为空时返回 <see langword="null"/>。</returns>
    internal static string DisposeSchemaType(OpenApiDocumentSchemaPropertyDto schema, HashSet<string> refSchemas = null)
    {
        if (schema == null)
        {
            return null;
        }

        string schemaType;
        if (!string.IsNullOrWhiteSpace(schema.Ref))
        {
            schemaType = DisposeSchemaRefKey(schema.Ref, refSchemas);
        }
        else if (schema.OneOf?.Count > 0)
        {
            schemaType = DisposeCompositeSchemaType(schema.OneOf, " | ", refSchemas);
        }
        else if (schema.AnyOf?.Count > 0)
        {
            schemaType = DisposeCompositeSchemaType(schema.AnyOf, " | ", refSchemas);
        }
        else if (schema.AllOf?.Count > 0)
        {
            schemaType = DisposeCompositeSchemaType(schema.AllOf, " & ", refSchemas);
        }
        else if (string.Equals(schema.Type, "array", StringComparison.OrdinalIgnoreCase))
        {
            string itemType = DisposeSchemaType(schema.Items, refSchemas) ?? "unknown";
            schemaType = itemType.Contains(" | ", StringComparison.Ordinal) || itemType.Contains(" & ", StringComparison.Ordinal)
                ? $"({itemType})[]"
                : $"{itemType}[]";
        }
        else if (TryDisposeAdditionalProperties(schema.AdditionalProperties, refSchemas, out string additionalPropertiesType))
        {
            schemaType = $"Record<string, {additionalPropertiesType}>";
        }
        else if (schema.Properties?.Count > 0)
        {
            IEnumerable<string> propertyTypes = schema.Properties.Select(property =>
            {
                string propertyName = JsonSerializer.Serialize(property.Key);
                string optional = schema.Required?.Contains(property.Key) == true ? "" : "?";
                string propertyType = DisposeSchemaType(property.Value, refSchemas) ?? "unknown";
                return $"{propertyName}{optional}: {propertyType};";
            });
            schemaType = $"{{ {string.Join(" ", propertyTypes)} }}";
        }
        else
        {
            schemaType = FindBaseTypeMapping(schema.Format) ?? DisposeBaseType(schema.Type);
        }

        if (string.IsNullOrWhiteSpace(schemaType))
        {
            return null;
        }

        return schemaType;
    }

    /// <summary>
    /// 将组件架构转换为通用属性架构。
    /// </summary>
    /// <param name="schema">组件架构。</param>
    /// <returns>通用属性架构。</returns>
    private static OpenApiDocumentSchemaPropertyDto ConvertSchema(OpenApiDocumentComponentSchemaDto schema)
    {
        return new OpenApiDocumentSchemaPropertyDto
        {
            Type = schema.Type,
            Format = schema.Format,
            Nullable = schema.Nullable,
            Ref = schema.Ref,
            Items = schema.Items,
            Properties = schema.Properties,
            Required = schema.Required,
            AdditionalProperties = schema.AdditionalPropertiesSchema,
            AllOf = schema.AllOf,
            AnyOf = schema.AnyOf,
            OneOf = schema.OneOf
        };
    }

    /// <summary>
    /// 递归处理架构引用名称。
    /// </summary>
    /// <param name="refName">架构引用名称。</param>
    /// <param name="refSchemas">用于解析引用的 OpenAPI 架构集合。</param>
    /// <returns>TypeScript 类型。</returns>
    private static string DisposeSchemaRefName(string refName, HashSet<string> refSchemas)
    {
        if (string.IsNullOrWhiteSpace(refName))
        {
            return null;
        }

        string baseType = FindBaseTypeMapping(refName);
        if (baseType != null)
        {
            return baseType;
        }

        OpenApiImportTypeMappingSettingsOptions typeMapping =
            Penetrates.OpenApiSettings.ImportTypeMappings.FirstOrDefault(mapping =>
                refName.StartsWith(mapping.Name, StringComparison.Ordinal));
        if (typeMapping == null)
        {
            refSchemas?.Add(refName);
            return refName;
        }

        string remainingName = refName[typeMapping.Name.Length..];
        string remainingType = DisposeSchemaRefName(remainingName, refSchemas);
        if (typeMapping.RefSchema?.Count > 0)
        {
            refSchemas?.UnionWith(typeMapping.RefSchema);
        }

        string result;
        if (typeMapping.MappingName == "{0}[]"
            && (remainingType?.Contains(" | ", StringComparison.Ordinal) == true
                || remainingType?.Contains(" & ", StringComparison.Ordinal) == true))
        {
            result = $"({remainingType})[]";
        }
        else if (typeMapping.MappingName == "readonly {0}[]"
                 && (remainingType?.Contains(" | ", StringComparison.Ordinal) == true
                     || remainingType?.Contains(" & ", StringComparison.Ordinal) == true))
        {
            result = $"readonly ({remainingType})[]";
        }
        else
        {
            result = string.Format(System.Globalization.CultureInfo.InvariantCulture, typeMapping.MappingName,
                remainingType ?? string.Empty);
        }

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    /// <summary>
    /// 查找基础类型映射。
    /// </summary>
    /// <param name="typeName">类型名称或 OpenAPI 格式。</param>
    /// <returns>TypeScript 类型；未找到时返回 <see langword="null"/>。</returns>
    private static string FindBaseTypeMapping(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        return Penetrates.OpenApiSettings.BaseTypeMappings.FirstOrDefault(mapping =>
                string.Equals(mapping.Key, typeName, StringComparison.OrdinalIgnoreCase))
            .Value;
    }

    /// <summary>
    /// 处理组合架构类型。
    /// </summary>
    /// <param name="schemas">组合架构。</param>
    /// <param name="separator">TypeScript 类型分隔符。</param>
    /// <param name="refSchemas">用于解析引用的 OpenAPI 架构集合。</param>
    /// <returns>TypeScript 组合类型。</returns>
    private static string DisposeCompositeSchemaType(IEnumerable<OpenApiDocumentSchemaPropertyDto> schemas, string separator,
        HashSet<string> refSchemas)
    {
        var schemaTypes = schemas.Where(schema => !string.Equals(schema.Type, "null", StringComparison.OrdinalIgnoreCase))
            .Select(schema => DisposeSchemaType(schema, refSchemas))
            .Where(type => !string.IsNullOrWhiteSpace(type) && type != "null")
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (schemaTypes.Count == 0)
        {
            return "unknown";
        }

        return string.Join(separator,
            schemaTypes.Select(type =>
                (separator == " & " && type.Contains(" | ", StringComparison.Ordinal))
                || (separator == " | " && type.Contains(" & ", StringComparison.Ordinal))
                    ? $"({type})"
                    : type));
    }

    /// <summary>
    /// 处理附加属性架构。
    /// </summary>
    /// <param name="additionalProperties">附加属性原始定义。</param>
    /// <param name="refSchemas">用于解析引用的 OpenAPI 架构集合。</param>
    /// <param name="type">附加属性的 TypeScript 类型。</param>
    /// <returns>存在附加属性定义时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    private static bool TryDisposeAdditionalProperties(JsonElement additionalProperties, HashSet<string> refSchemas,
        out string type)
    {
        type = null;
        switch (additionalProperties.ValueKind)
        {
            case JsonValueKind.True:
                type = "unknown";
                return true;
            case JsonValueKind.Object:
                OpenApiDocumentSchemaPropertyDto schema =
                    additionalProperties.Deserialize<OpenApiDocumentSchemaPropertyDto>(_openApiSerializerOptions);
                type = DisposeSchemaType(schema, refSchemas) ?? "unknown";
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 生成声明导入
    /// </summary>
    /// <param name="hasWeb">是否为 Web 端</param>
    /// <param name="dirName">文件夹名称</param>
    /// <param name="refSchemas">引用声明</param>
    /// <param name="enumSchemas">枚举声明</param>
    /// <returns>生成的外部声明导入、本地声明导入和本地引用声明</returns>
    internal static (List<string> externalImports, List<string> schemaImports, HashSet<string> refSchemas) GenerateSchemaImport(
        bool hasWeb, string dirName, HashSet<string> refSchemas, List<ComponentSchemaDto> enumSchemas)
    {
        if (refSchemas == null || refSchemas.Count == 0)
        {
            return ([], [], []);
        }

        var externalImports = new List<string>();
        var schemaImports = new List<string>();
        var newRefSchemas = new HashSet<string>();

        var schemaMapping = Penetrates.OpenApiSettings.ImportSchemaMappings.Where(wh => refSchemas.Contains(wh.Name)).ToList();
        if (schemaMapping.Count != 0)
        {
            var schemaMappingGroup = schemaMapping.GroupBy(gb => hasWeb ? gb.WebImportPath : gb.MobileImportPath).ToList();
            foreach (IGrouping<string, OpenApiImportSchemaMappingSettingsOptions> item in schemaMappingGroup.OrderBy(ob => ob.Key,
                         StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(item.Key))
                {
                    string importNames = string.Join(", ",
                        item.OrderBy(ob => ob.Name, StringComparer.Ordinal).Select(sl => sl.Name));
                    externalImports.Add($$"""import type { {{importNames}} } from "{{item.Key}}";""");
                }
            }
        }

        foreach (string refSchema in refSchemas.Where(wh => schemaMapping.All(a => a.Name != wh))
                     .Where(wh => enumSchemas.All(a => a.Name != wh))
                     .OrderBy(ob => ob, StringComparer.OrdinalIgnoreCase))
        {
            string importPath = string.IsNullOrWhiteSpace(dirName) ? $"./{refSchema}" : $"./{dirName}/{refSchema}";
            schemaImports.Add($$"""import type { {{refSchema}} } from "{{importPath}}";""");
            newRefSchemas.Add(refSchema);
        }

        schemaImports.AddRange(enumSchemas.Where(wh => refSchemas.Contains(wh.Name))
            .OrderBy(ob => ob.Name, StringComparer.OrdinalIgnoreCase)
            .Select(sl => sl.ImportPath));

        return (externalImports, schemaImports, newRefSchemas);
    }

    /// <summary>
    /// 生成 OpenAPI 文档声明文件
    /// </summary>
    /// <param name="openApiDocument">OpenAPI 文档</param>
    /// <param name="scriptLanguage">脚本语言</param>
    /// <returns>表示异步生成 OpenAPI 文档声明文件的任务，任务结果为生成的 OpenAPI 文档声明文件集合</returns>
    internal static async Task<List<ComponentSchemaDto>> GenerateOpenApiDocumentSchemaFile(OpenApiDocumentDto openApiDocument,
        ScriptLanguageEnum scriptLanguage)
    {
        if (openApiDocument.Components?.Schemas == null)
        {
            return null;
        }

        var result = new List<ComponentSchemaDto>();

        // JavaScript 版本不生成 TypeScript 类型声明
        if (scriptLanguage == ScriptLanguageEnum.JavaScript)
        {
            return result;
        }

        try
        {
            // 获取文档 Dto 声明
            var dtoSchemas = openApiDocument.Components.Schemas.Where(wh => wh.Value.Enum == null).ToList();

            foreach (KeyValuePair<string, OpenApiDocumentComponentSchemaDto> dtoSchema in dtoSchemas)
            {
                if (string.IsNullOrWhiteSpace(dtoSchema.Key))
                {
                    continue;
                }

                if (dtoSchema.Value == null)
                {
                    continue;
                }

                if (result.Any(a => a.Name == dtoSchema.Key))
                {
                    continue;
                }

                // 判断是否为忽略声明
                if (Penetrates.OpenApiSettings.IgnoreSchemas.Contains(dtoSchema.Key))
                {
                    continue;
                }

                // 判断是否为导入声明映射 Name
                if (Penetrates.OpenApiSettings.ImportSchemaMappings.Any(a =>
                        dtoSchema.Key.StartsWith(a.Name, StringComparison.Ordinal)))
                {
                    continue;
                }

                // 判断是否为导入类型映射 Name
                if (Penetrates.OpenApiSettings.ImportTypeMappings.Any(a =>
                        dtoSchema.Key.StartsWith(a.Name, StringComparison.Ordinal)))
                {
                    continue;
                }

                // 判断是否为基类
                if (Penetrates.OpenApiSettings.BaseTypeMappings.Any(a => a.Key == dtoSchema.Key))
                {
                    continue;
                }

                var schemaDto = new ComponentSchemaDto {Name = dtoSchema.Key, Content = new StringBuilder(), RefSchemas = []};

                string schemaDescription = dtoSchema.Value.Description?.Replace("\r\n", "\r\n * ");

                IDictionary<string, OpenApiDocumentSchemaPropertyDto> properties =
                    dtoSchema.Value.Properties ?? new Dictionary<string, OpenApiDocumentSchemaPropertyDto>();
                OpenApiDocumentSchemaPropertyDto componentSchema = ConvertSchema(dtoSchema.Value);
                bool generateTypeAlias = properties.Count == 0
                                         && (!string.IsNullOrWhiteSpace(componentSchema.Ref)
                                             || componentSchema.Items != null
                                             || componentSchema.AllOf?.Count > 0
                                             || componentSchema.AnyOf?.Count > 0
                                             || componentSchema.OneOf?.Count > 0
                                             || componentSchema.AdditionalProperties.ValueKind is JsonValueKind.True
                                                 or JsonValueKind.Object
                                             || (!string.IsNullOrWhiteSpace(componentSchema.Type)
                                                 && !string.Equals(componentSchema.Type, "unknown",
                                                     StringComparison.OrdinalIgnoreCase)));
                if (generateTypeAlias)
                {
                    string schemaType = DisposeSchemaType(componentSchema, schemaDto.RefSchemas) ?? "unknown";
                    schemaDto.Content.Append($"""
                                              /**
                                               * {schemaDescription}
                                               */
                                              export type {dtoSchema.Key} = {schemaType};

                                              """);
                    result.Add(schemaDto);
                    continue;
                }

                schemaDto.Content.Append($"""
                                          /**
                                           * {schemaDescription}
                                           */
                                          export interface {dtoSchema.Key}
                                          """);

                // 判断是否存在分页
                bool hasPaged = Penetrates.OpenApiSettings.PagedSchemaProperties.All(properties.ContainsKey);
                if (hasPaged)
                {
                    schemaDto.Content.Append(" extends PagedInput ");
                    schemaDto.RefSchemas.Add("PagedInput");
                }

                schemaDto.Content.Append(" {");

                // 属性
                foreach (KeyValuePair<string, OpenApiDocumentSchemaPropertyDto> property in properties)
                {
                    // 判断是否为分页属性
                    if (Penetrates.OpenApiSettings.PagedSchemaProperties.Contains(property.Key))
                    {
                        continue;
                    }

                    // 获取属性描述
                    string propertyDescription = property
                        .Value.Description?.Replace("\r\n\r\n", "\r\n")
                        .Replace("\r\n", "\r\n	 * ");

                    schemaDto.Content.Append(Environment.NewLine);
                    schemaDto.Content.Append($"""
                                                /**
                                                 * {propertyDescription}
                                                 */
                                                {(property.Value.ReadOnly ? "readonly " : "")}{property.Key}?: 
                                              """);

                    string propertyType = DisposeSchemaType(property.Value, schemaDto.RefSchemas) ?? "unknown";
                    schemaDto.Content.Append($"{propertyType};");
                }

                schemaDto.Content.Append(Environment.NewLine);
                schemaDto.Content.AppendLine("}");
                schemaDto.Content.Append(Environment.NewLine);

                result.Add(schemaDto);
            }
        }
        catch (Exception ex)
        {
            MAppContext.ConsoleWrite(console =>
            {
                console.BackgroundColor = ConsoleColor.DarkRed;
                console.ForegroundColor = ConsoleColor.Black;
                console.Write("fail");
                console.ResetColor();
                console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                console.BackgroundColor = ConsoleColor.DarkRed;
                console.ForegroundColor = ConsoleColor.Black;
                console.WriteLine($"      生成 {openApiDocument.Url} {scriptLanguage.ToString()} 声明文件失败...");
                console.WriteLine($"      {ex}");
            });
        }

        await Task.CompletedTask;

        return result;
    }

    /// <summary>
    /// 写入 OpenAPI 文档声明文件
    /// </summary>
    /// <param name="hasWeb">是否为 Web 端</param>
    /// <param name="rootDir">根目录</param>
    /// <param name="openApiDocument">OpenAPI 文档</param>
    /// <param name="schemaDto">声明</param>
    /// <param name="dtoSchemas">DTO 声明</param>
    /// <param name="enumSchemas">枚举声明</param>
    /// <param name="scriptLanguage">脚本语言</param>
    /// <returns>表示异步写入 OpenAPI 文档声明文件的任务</returns>
    internal static async Task WriteOpenApiDocumentSchemaFile(bool hasWeb, string rootDir, OpenApiDocumentDto openApiDocument,
        ComponentSchemaDto schemaDto, List<ComponentSchemaDto> dtoSchemas, List<ComponentSchemaDto> enumSchemas,
        ScriptLanguageEnum scriptLanguage)
    {
        // JavaScript 版本不生成 TypeScript 类型声明
        if (scriptLanguage == ScriptLanguageEnum.JavaScript)
        {
            return;
        }

        try
        {
            // 排除递归的问题
            var refSchemas = schemaDto.RefSchemas.Where(wh => wh != schemaDto.Name).ToList();

            (List<string> externalImports, List<string> schemaImports, HashSet<string> newRefSchemas) =
                GenerateSchemaImport(hasWeb, null, refSchemas.ToHashSet(), enumSchemas);
            var imports = externalImports.Concat(schemaImports).ToList();
            string content = imports.Count > 0
                ? $"""
                   {string.Join(Environment.NewLine, imports)}

                   {schemaDto.Content}
                   """
                : $"{schemaDto.Content}";

            await File.WriteAllTextAsync(Path.Combine(rootDir, $"{schemaDto.Name}.ts"), FormatScriptContent(content));


            // 处理引用文件
            foreach (string refSchema in newRefSchemas)
            {
                // 从声明集合中查找
                ComponentSchemaDto childrenSchemaDto = dtoSchemas.Single(s => s.Name == refSchema);
                await WriteOpenApiDocumentSchemaFile(hasWeb, rootDir, openApiDocument, childrenSchemaDto, dtoSchemas, enumSchemas,
                    scriptLanguage);
            }
        }
        catch (Exception ex)
        {
            MAppContext.ConsoleWrite(console =>
            {
                console.BackgroundColor = ConsoleColor.DarkRed;
                console.ForegroundColor = ConsoleColor.Black;
                console.Write("fail");
                console.ResetColor();
                console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                console.BackgroundColor = ConsoleColor.DarkRed;
                console.ForegroundColor = ConsoleColor.Black;
                console.WriteLine($"      写入 {openApiDocument.Url} {scriptLanguage.ToString()} 声明文件失败...");
                console.WriteLine($"      {ex}");
            });
        }
    }
}
