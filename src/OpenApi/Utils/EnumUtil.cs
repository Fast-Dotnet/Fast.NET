// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI 枚举工具类
/// </summary>
public static partial class OpenApiUtil
{
    /// <summary>
    /// 写入 OpenAPI 文档枚举文件
    /// </summary>
    /// <param name="rootDir">根目录</param>
    /// <param name="openApiDocument">OpenAPI 文档</param>
    /// <param name="scriptLanguage">脚本语言</param>
    /// <returns>表示异步写入 OpenAPI 文档枚举文件的任务，任务结果为写入 OpenAPI 文档枚举文件集合</returns>
    internal static async Task<List<ComponentSchemaDto>> WriteOpenApiDocumentEnumFile(string rootDir,
        OpenApiDocumentDto openApiDocument, ScriptLanguageEnum scriptLanguage)
    {
        if (openApiDocument.Components.Schemas == null)
        {
            return null;
        }

        var result = new List<ComponentSchemaDto>();

        try
        {
            // 获取所有有效枚举类型
            var enumTypeList = MAppContext.EffectiveTypes.Where(wh => wh.IsEnum).ToList();
            // 获取文档枚举声明
            var enumSchemas = openApiDocument.Components.Schemas.Where(wh => wh.Value.Enum != null).ToList();

            foreach (KeyValuePair<string, OpenApiDocumentComponentSchemaDto> enumSchema in enumSchemas)
            {
                // 根据名称查找枚举类型
                Type enumType = enumTypeList.Where(t => t.Name == enumSchema.Key)
                    .OrderBy(t =>
                    {
                        string name = t.Assembly.GetName().Name;
                        return name?.StartsWith(nameof(System), StringComparison.Ordinal) == true
                               || name?.StartsWith(nameof(Microsoft), StringComparison.Ordinal) == true;
                    })
                    .FirstOrDefault();
                if (enumType == null)
                {
                    continue;
                }

                // 判断是否忽略
                if (Penetrates.OpenApiSettings.IgnoreSchemas.Any(a => a == enumType.Name))
                {
                    continue;
                }

                if (result.Any(a => a.Name == enumType.Name))
                {
                    continue;
                }

                // 通过 Type.GetTypeCode() 获取底层类型的 TypeCode，判断基础类型
                TypeCode typeCode = Type.GetTypeCode(enumType);
                bool hasLong = typeCode == TypeCode.Int64;

                string[] enumNames = Enum.GetNames(enumType);
                Array enumValues = Enum.GetValues(enumType);
                var enumDetail = new StringBuilder();

                for (int i = 0; i < enumValues.Length; i++)
                {
                    string enumName = enumNames[i];
                    // 获取枚举值
                    string enumValue = Convert.ToInt64(enumValues.GetValue(i)).ToString();
                    if (string.IsNullOrWhiteSpace(enumValue))
                    {
                        continue;
                    }

                    // 判断是否为 long 类型枚举
                    if (hasLong)
                    {
                        enumValue = @$"""{enumValue}""";
                    }

                    // 获取枚举描述，如果为空则默认使用 Name
                    string enumDescription = enumType
                                                 .GetField(enumName)
                                                 ?.GetCustomAttribute<DescriptionAttribute>(false)
                                                 ?.Description
                                             ?? enumName;

                    switch (scriptLanguage)
                    {
                        case ScriptLanguageEnum.JavaScript:
                            enumDetail.Append($"""
                                                 /**
                                                  * {enumDescription}
                                                  */
                                                 {enumName}: {enumValue},
                                               """);
                            break;
                        case ScriptLanguageEnum.TypeScript:
                            enumDetail.Append($"""
                                                 /**
                                                  * {enumDescription}
                                                  */
                                                 {enumName} = {enumValue},
                                               """);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(scriptLanguage), scriptLanguage, null);
                    }

                    // 拼接换行符
                    if (i + 1 != enumValues.Length)
                    {
                        enumDetail.Append(Environment.NewLine);
                    }
                }

                string schemaDescription = enumSchema.Value.Description?.Replace("\r\n", "\r\n * ")
                                           ?? enumType.GetCustomAttribute<FastEnumAttribute>()?.ChName;

                switch (scriptLanguage)
                {
                    case ScriptLanguageEnum.JavaScript:
                        await File.WriteAllTextAsync(Path.Combine(rootDir, $"{enumType.Name}.js"), FormatScriptContent($$"""
                              /**
                               * {{schemaDescription}}
                               */
                              export const {{enumType.Name}} = {
                              {{enumDetail}}
                              };
                              """));

                        result.Add(new ComponentSchemaDto
                        {
                            Name = enumType.Name,
                            ImportPath = $$"""import { {{enumType.Name}} } from "@/api/enums/{{enumType.Name}}.js";"""
                        });
                        break;
                    case ScriptLanguageEnum.TypeScript:
                        await File.WriteAllTextAsync(Path.Combine(rootDir, $"{enumType.Name}.ts"), FormatScriptContent($$"""
                              /**
                               * {{schemaDescription}}
                               */
                              export enum {{enumType.Name}} {
                              {{enumDetail}}
                              }

                              """));

                        result.Add(new ComponentSchemaDto
                        {
                            Name = enumType.Name,
                            ImportPath = $$"""import type { {{enumType.Name}} } from "@/api/enums/{{enumType.Name}}";"""
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(scriptLanguage), scriptLanguage, null);
                }
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
                console.WriteLine($"      写入 {openApiDocument.Url} {scriptLanguage.ToString()} 枚举文件失败...");
                console.WriteLine($"      {ex}");
            });
        }

        return result;
    }
}
