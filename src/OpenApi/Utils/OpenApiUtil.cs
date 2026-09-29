// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI 工具类
/// </summary>
public static partial class OpenApiUtil
{
    /// <summary>
    /// 生成 OpenAPI 文档资源
    /// </summary>
    /// <param name="address">目标服务地址</param>
    /// <param name="apiDescriptionGroupCollectionProvider">用于读取所有 API 描述分组的提供器</param>
    /// <param name="groupList">文档分组集合</param>
    /// <returns>表示异步“生成 OpenAPI 文档资源”操作的任务</returns>
    public static async Task GenerateOpenApi(string address,
        IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider, List<string> groupList = null)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("OpenAPI 服务地址不能为空。", nameof(address));
        }

        if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri))
        {
            throw new ArgumentException("OpenAPI 服务地址必须是有效的绝对地址。", nameof(address));
        }

        ArgumentNullException.ThrowIfNull(apiDescriptionGroupCollectionProvider);

        try
        {
            MAppContext.ConsoleWrite(console =>
            {
                console.BackgroundColor = ConsoleColor.Black;
                console.ForegroundColor = ConsoleColor.Green;
                console.Write("info");
                console.ResetColor();
                console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                console.BackgroundColor = ConsoleColor.Black;
                console.ForegroundColor = ConsoleColor.DarkGray;
                console.WriteLine("      开始生成 Open Api 文件...");
            });

            // 复制调用方集合，避免为补充默认分组而意外修改外部状态
            List<string> groups = groupList?.ToList() ?? ["All Groups"];
            // 增加默认分组
            if (!groups.Contains("Default", StringComparer.Ordinal))
            {
                groups.Add("Default");
            }

            // 根目录
            string rootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fast.OpenApi");
            if (Directory.Exists(rootDir))
            {
                // 每次确保都是最新的
                Directory.Delete(rootDir, true);
            }

            Directory.CreateDirectory(rootDir);

            foreach (string group in groups)
            {
                // 获取文档地址
                string url = $"{address.TrimEnd('/')}/swagger/{group}/swagger.json";

                // 获取文档信息
                OpenApiDocumentDto openApiDocument = await GetOpenApiDocument(url).ConfigureAwait(false);
                if (openApiDocument == null)
                {
                    continue;
                }

                // JavaScript
                await GenerateOpenApi(apiDescriptionGroupCollectionProvider, openApiDocument, rootDir, group, uri, true,
                        ScriptLanguageEnum.JavaScript)
                    .ConfigureAwait(false);
                await GenerateOpenApi(apiDescriptionGroupCollectionProvider, openApiDocument, rootDir, group, uri, false,
                        ScriptLanguageEnum.JavaScript)
                    .ConfigureAwait(false);

                // TypeScript
                await GenerateOpenApi(apiDescriptionGroupCollectionProvider, openApiDocument, rootDir, group, uri, true,
                        ScriptLanguageEnum.TypeScript)
                    .ConfigureAwait(false);
                await GenerateOpenApi(apiDescriptionGroupCollectionProvider, openApiDocument, rootDir, group, uri, false,
                        ScriptLanguageEnum.TypeScript)
                    .ConfigureAwait(false);
            }

            MAppContext.ConsoleWrite(console =>
            {
                console.BackgroundColor = ConsoleColor.Black;
                console.ForegroundColor = ConsoleColor.Green;
                console.Write("info");
                console.ResetColor();
                console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                console.BackgroundColor = ConsoleColor.Black;
                console.ForegroundColor = ConsoleColor.DarkGray;
                console.WriteLine("      生成 Open Api 文件成功。");
            });
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
                console.WriteLine("      生成 Open Api 文件失败...");
                console.WriteLine($"      {ex}");
            });
            throw;
        }
    }

    /// <summary>
    /// 生成 OpenAPI 文档资源
    /// </summary>
    /// <param name="apiDescriptionGroupCollectionProvider">接口描述提供程序</param>
    /// <param name="openApiDocument">OpenAPI 文档</param>
    /// <param name="rootDir">根目录</param>
    /// <param name="group">分组</param>
    /// <param name="uri">地址</param>
    /// <param name="hasWeb">是否为 Web 端</param>
    /// <param name="scriptLanguage">脚本语言</param>
    /// <returns>表示异步生成 OpenAPI 文档资源的任务</returns>
    internal static async Task GenerateOpenApi(IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider,
        OpenApiDocumentDto openApiDocument, string rootDir, string group, Uri uri, bool hasWeb, ScriptLanguageEnum scriptLanguage)
    {
        // 判断是否存在路由
        if (openApiDocument.Paths.Count == 0)
        {
            return;
        }

        // 当前文档地址
        string curRootDir = Path.Combine(rootDir,
            $"{uri.Host}_{uri.Port}_{(hasWeb ? "Web" : "Mobile")}_{scriptLanguage.ToString()}");
        Directory.CreateDirectory(curRootDir);

        // 枚举文件
        string enumRootDir = Path.Combine(curRootDir, "enums");
        Directory.CreateDirectory(enumRootDir);

        // API 文件
        string apiRootDir = Penetrates.OpenApiSettings.FolderGroup == true
            ? Path.Combine(curRootDir, "services", group)
            : Path.Combine(curRootDir, "services");
        Directory.CreateDirectory(apiRootDir);

        // 写入枚举
        List<ComponentSchemaDto> enumSchemas = await WriteOpenApiDocumentEnumFile(enumRootDir, openApiDocument, scriptLanguage)
            .ConfigureAwait(false);

        // 生成 Dto
        List<ComponentSchemaDto> dtoSchemas = await GenerateOpenApiDocumentSchemaFile(openApiDocument, scriptLanguage)
            .ConfigureAwait(false);

        // 写入 API
        await WriteOpenApiDocumentApiFile(apiRootDir, hasWeb, apiDescriptionGroupCollectionProvider, openApiDocument, dtoSchemas,
                enumSchemas, scriptLanguage)
            .ConfigureAwait(false);
    }
}
