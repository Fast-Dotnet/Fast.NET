// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI API 工具类
/// </summary>
public static partial class OpenApiUtil
{
    /// <summary>
    /// 处理请求行为
    /// </summary>
    /// <param name="requestAction">请求行为</param>
    /// <returns>处理请求行为</returns>
    internal static string DisposeRequestAction(HttpRequestActionEnum requestAction)
    {
        switch (requestAction)
        {
            case HttpRequestActionEnum.Auth:
                return "auth";
            case HttpRequestActionEnum.Paged:
            case HttpRequestActionEnum.Query:
                return "query";
            case HttpRequestActionEnum.Add:
                return "add";
            case HttpRequestActionEnum.Edit:
                return "edit";
            case HttpRequestActionEnum.Delete:
                return "delete";
            case HttpRequestActionEnum.Submit:
                return "submit";
            case HttpRequestActionEnum.Upload:
                return "upload";
            case HttpRequestActionEnum.Download:
                return "download";
            case HttpRequestActionEnum.Export:
                return "export";
            case HttpRequestActionEnum.Import:
                return "import";
            case HttpRequestActionEnum.Callback:
                return "callback";
            case HttpRequestActionEnum.None:
            case HttpRequestActionEnum.Notify:
            case HttpRequestActionEnum.Other:
            default:
                return "other";
        }
    }

    /// <summary>
    /// 写入 OpenAPI 文档 API 文件
    /// </summary>
    /// <param name="rootDir">根目录</param>
    /// <param name="hasWeb">是否为 Web 端</param>
    /// <param name="apiDescriptionGroupCollectionProvider">接口描述提供程序</param>
    /// <param name="openApiDocument">OpenAPI 文档</param>
    /// <param name="dtoSchemas">DTO 声明</param>
    /// <param name="enumSchemas">枚举声明</param>
    /// <param name="scriptLanguage">脚本语言</param>
    /// <returns>表示异步写入 OpenAPI 文档 API 文件的任务</returns>
    internal static async Task WriteOpenApiDocumentApiFile(string rootDir, bool hasWeb,
        IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider, OpenApiDocumentDto openApiDocument,
        List<ComponentSchemaDto> dtoSchemas, List<ComponentSchemaDto> enumSchemas, ScriptLanguageEnum scriptLanguage)
    {
        try
        {
            // 获取所有 Tag
            var tagList = openApiDocument.Paths.Where(wh => wh.Value.Tag != null).Select(sl => sl.Value.Tag).Distinct().ToList();

            // 获取所有接口描述
            var apiDescriptions = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items.SelectMany(sl => sl.Items)
                .ToList();

            foreach (string tag in tagList)
            {
                // 处理 xxx/xxx 这种 tag
                string tagName = tag;
                string[] tagSplit = tag.Split("/", StringSplitOptions.RemoveEmptyEntries);
                if (tagSplit.Length > 1)
                {
                    tagName = tagSplit[0] + string.Concat(tagSplit.Skip(1).Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
                }

                // 创建 api 文件夹
                string apiFileDir = Path.Combine(rootDir, tagName);
                Directory.CreateDirectory(apiFileDir);

                // 获取当前 tag 下所有的接口
                var curPaths = openApiDocument.Paths.Where(wh => wh.Value.Tag == tag).ToList();

                // 模块描述
                string tagDescription = openApiDocument.Tags?.SingleOrDefault(s => s.Name == tag)?.Description;

                var contentSb = new StringBuilder();
                // 引用声明
                var refSchemas = new HashSet<string>();
                // 仅在当前模块包含上传接口时生成 Axios 上传进度类型导入。
                bool hasUpload = false;
                // 仅在当前模块包含下载或导出接口时生成 Axios 响应类型导入。
                bool hasFileDownload = false;

                for (int i = 0; i < curPaths.Count; i++)
                {
                    OpenApiDocumentPathDto apiInfo = curPaths[i].Value;
                    string apiName = curPaths[i].Key;
                    // 判断是否为路由格式的接口，如果是则不生成接口文档
                    if (Regex.IsMatch(apiName, @"\/\{[a-zA-Z0-9_]+\}"))
                    {
                        continue;
                    }

                    string apiFuncName = apiName.Split("/").LastOrDefault();

                    // 获取接口描述
                    ApiDescription apiDescription = apiDescriptions.Single(s => $"/{s.RelativePath}" == apiName);

                    // 获取请求特性
                    ApiInfoAttribute apiInfoAttribute = apiDescription.ActionDescriptor.EndpointMetadata
                        .OfType<ApiInfoAttribute>()
                        .FirstOrDefault();

                    // 获取请求类型
                    HttpRequestActionEnum apiActionEnum = apiInfoAttribute?.Action ?? HttpRequestActionEnum.Other;
                    string apiAction = DisposeRequestAction(apiActionEnum);

                    // 响应数据类型
                    string responseType = DisposeSchemaType(apiInfo.Method.Responses?.Code200?.Content?.Json?.Schema, refSchemas);
                    if (apiActionEnum is HttpRequestActionEnum.Download or HttpRequestActionEnum.Export)
                    {
                        responseType = hasWeb ? "AxiosResponse<Blob>" : "AxiosResponse<Blob | ArrayBuffer | string>";
                        hasFileDownload = true;
                    }
                    else if (string.IsNullOrWhiteSpace(responseType)
                             && apiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
                             && (controllerActionDescriptor.MethodInfo.ReturnType == typeof(Task)
                                 || controllerActionDescriptor.MethodInfo.ReturnType == typeof(ValueTask)))
                    {
                        responseType = "void";
                    }

                    OpenApiDocumentPathMethodDto methodInfo = apiInfo.Method;
                    bool isFormData = methodInfo?.RequestBody?.Content?.FormData != null;
                    bool isMobileUpload = !hasWeb && isFormData;
                    if (isFormData)
                    {
                        hasUpload = true;
                    }

                    // 获取接口名称（注释）
                    string apiSummary = apiInfoAttribute?.Name ?? methodInfo.Summary;

                    contentSb.Append($"""
                                        /**
                                         * {apiSummary}
                                         */
                                        {apiFuncName}(
                                      """);

                    // 请求参数
                    string requestParam = "";
                    var requestParamSb = new StringBuilder();

                    // 请求体类型
                    string requestDataType = "";

                    if (isFormData)
                    {
                        requestDataType = "FormData";
                    }
                    else if (methodInfo?.RequestBody?.Content?.Json?.Schema != null)
                    {
                        requestDataType = DisposeSchemaType(methodInfo.RequestBody.Content.Json.Schema, refSchemas);
                    }

                    if (methodInfo?.Parameters != null)
                    {
                        for (int j = 0; j < methodInfo.Parameters.Count; j++)
                        {
                            OpenApiDocumentPathMethodParameterDto parameter = methodInfo.Parameters[j];

                            switch (scriptLanguage)
                            {
                                case ScriptLanguageEnum.JavaScript:
                                    requestParam += $"{parameter.Name}, ";
                                    break;
                                case ScriptLanguageEnum.TypeScript:
                                    string parameterType = DisposeSchemaType(parameter?.Schema, refSchemas) ?? "unknown";
                                    requestParam += $"{parameter.Name}: {parameterType}, ";

                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(scriptLanguage), scriptLanguage, null);
                            }

                            requestParamSb.Append($"        {parameter.Name},");

                            if (j + 1 != methodInfo.Parameters.Count)
                            {
                                requestParamSb.Append(Environment.NewLine);
                            }
                        }

                        // 处理可能存在 URL 参数和 Body 参数的情况
                        if (string.IsNullOrWhiteSpace(requestDataType))
                        {
                            requestParam = requestParam.TrimEnd(' ').TrimEnd(',');
                        }

                        contentSb.Append(requestParam);
                    }

                    if (!string.IsNullOrWhiteSpace(requestDataType))
                    {
                        switch (scriptLanguage)
                        {
                            case ScriptLanguageEnum.JavaScript:
                                if (isMobileUpload)
                                {
                                    // 移动端使用 filePath 参数，并按字符串处理
                                    contentSb.Append("filePath");
                                }
                                else
                                {
                                    contentSb.Append("data");
                                }

                                break;
                            case ScriptLanguageEnum.TypeScript:
                                if (isMobileUpload)
                                {
                                    // 移动端使用 filePath 参数，并按字符串处理
                                    contentSb.Append("filePath: string");
                                }
                                else
                                {
                                    contentSb.Append($"data: {requestDataType}");
                                }

                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(scriptLanguage), scriptLanguage, null);
                        }

                        if (isFormData)
                        {
                            contentSb.Append(scriptLanguage == ScriptLanguageEnum.TypeScript
                                ? ", onUploadProgress?: (progressEvent: AxiosProgressEvent) => void"
                                : ", onUploadProgress");
                        }
                    }

                    if (apiActionEnum is HttpRequestActionEnum.Download or HttpRequestActionEnum.Export)
                    {
                        if (methodInfo?.Parameters?.Count > 0 || !string.IsNullOrWhiteSpace(requestDataType))
                        {
                            contentSb.Append(", ");
                        }

                        contentSb.Append("autoDownloadFile = true");
                    }

                    contentSb.Append(scriptLanguage == ScriptLanguageEnum.TypeScript
                        ? $$"""
                            ): Promise<{{(string.IsNullOrWhiteSpace(responseType) ? "unknown" : responseType)}}> {
                                return axiosUtil.request
                            """
                        : """
                          ) {
                              return axiosUtil.request
                          """);

                    if (!string.IsNullOrWhiteSpace(responseType) && scriptLanguage == ScriptLanguageEnum.TypeScript)
                    {
                        contentSb.Append($"<{responseType}>");
                    }

                    contentSb.Append($$"""
                                       ({
                                             url: "{{apiName}}",
                                       """);
                    contentSb.Append(Environment.NewLine);

                    if (isMobileUpload)
                    {
                        // 移动端默认使用 upload
                        contentSb.Append("""
                                               method: "upload",
                                         """);
                    }
                    else
                    {
                        contentSb.Append($$"""
                                                 method: "{{apiDescription.HttpMethod.ToLowerInvariant()}}",
                                           """);
                    }

                    contentSb.Append(Environment.NewLine);

                    if (requestParamSb.Length > 0)
                    {
                        contentSb.Append($$"""
                                                 params: {
                                           {{requestParamSb}}
                                                 },
                                           """);
                        contentSb.Append(Environment.NewLine);
                    }

                    if (!string.IsNullOrWhiteSpace(requestDataType))
                    {
                        if (isMobileUpload)
                        {
                            // 移动端使用 filePath 参数，并按字符串处理
                            contentSb.Append("""
                                                   name: "file",
                                             """);
                            contentSb.Append(Environment.NewLine);
                            contentSb.Append("      filePath,");
                            contentSb.Append(Environment.NewLine);
                        }
                        else
                        {
                            contentSb.Append("      data,");
                            contentSb.Append(Environment.NewLine);
                        }

                        if (isFormData)
                        {
                            contentSb.Append("      onUploadProgress,");
                            contentSb.Append(Environment.NewLine);
                        }
                    }

                    // 处理下载和导出
                    if (apiActionEnum is HttpRequestActionEnum.Download or HttpRequestActionEnum.Export)
                    {
                        contentSb.Append("""
                                               responseType: "blob",
                                               autoDownloadFile,
                                         """);
                        contentSb.Append(Environment.NewLine);
                    }

                    // 处理文件上传重复请求
                    if (apiActionEnum is HttpRequestActionEnum.Upload)
                    {
                        contentSb.Append("""
                                               cancelDuplicateRequest: false,
                                         """);
                        contentSb.Append(Environment.NewLine);
                    }

                    contentSb.Append($$"""
                                             requestType: "{{apiAction}}",
                                           });
                                         },
                                       """);

                    if (i + 1 != curPaths.Count)
                    {
                        contentSb.Append(Environment.NewLine);
                    }
                }

                switch (scriptLanguage)
                {
                    case ScriptLanguageEnum.JavaScript:
                        await File.WriteAllTextAsync(Path.Combine(apiFileDir, "index.js"), FormatScriptContent($$"""
                              import { axiosUtil } from "@fast-china/axios";

                              /**
                               * {{tagDescription}}Api
                               */
                              export const {{tagName}}Api = {
                              {{contentSb}}
                              };

                              """));
                        break;
                    case ScriptLanguageEnum.TypeScript:
                        // 生成 import
                        (List<string> externalImports, List<string> schemaImports, HashSet<string> newRefSchemas) =
                            GenerateSchemaImport(hasWeb, "models", refSchemas, enumSchemas);
                        if (newRefSchemas.Count > 0)
                        {
                            // 创建 model 文件
                            string importFileDir = Path.Combine(apiFileDir, "models");
                            Directory.CreateDirectory(importFileDir);

                            foreach (ComponentSchemaDto dtoSchema in dtoSchemas.Where(wh => newRefSchemas.Contains(wh.Name))
                                         .ToList())
                            {
                                // 写入 import 文件
                                await WriteOpenApiDocumentSchemaFile(hasWeb, importFileDir, openApiDocument, dtoSchema,
                                    dtoSchemas, enumSchemas, ScriptLanguageEnum.TypeScript);
                            }
                        }

                        var imports = new List<string> {"import { axiosUtil } from \"@fast-china/axios\";"};
                        var axiosTypeImports = new List<string>();
                        if (hasUpload)
                        {
                            axiosTypeImports.Add("AxiosProgressEvent");
                        }

                        if (hasFileDownload)
                        {
                            axiosTypeImports.Add("AxiosResponse");
                        }

                        if (axiosTypeImports.Count > 0)
                        {
                            imports.Add($"import type {{ {string.Join(", ", axiosTypeImports)} }} from \"axios\";");
                        }

                        imports.AddRange(externalImports);
                        imports.AddRange(schemaImports);
                        await File.WriteAllTextAsync(Path.Combine(apiFileDir, "index.ts"), FormatScriptContent($$"""
                              {{string.Join(Environment.NewLine, imports)}}

                              /**
                               * {{tagDescription}}Api
                               */
                              export const {{tagName}}Api = {
                              {{contentSb}}
                              };

                              """));

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
                console.WriteLine($"      写入 {openApiDocument.Url} {scriptLanguage.ToString()} Api文件失败...");
                console.WriteLine($"      {ex}");
            });
        }
    }

    /// <summary>
    /// 格式化生成的脚本内容
    /// </summary>
    /// <param name="content">脚本内容</param>
    /// <returns>使用 Tab 缩进和 LF 换行符的脚本内容</returns>
    internal static string FormatScriptContent(string content)
    {
        string normalizedContent = content.Replace("\r\n", "\n").Replace('\r', '\n');
        return Regex.Replace(normalizedContent, @"(?m)^(?: {2})+", match => new string('\t', match.Value.Length / 2));
    }
}
