// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI 请求工具类
/// </summary>
public static partial class OpenApiUtil
{
    private static readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions _openApiSerializerOptions = new()
    {
        // 忽略 OpenAPI 对象图中的循环引用
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        // 显式指定 UTF-8 JSON 媒体类型，避免响应编码被错误推断
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 获取 OpenAPI 文档信息
    /// </summary>
    /// <param name="documentUrl">文档地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>表示异步获取 OpenAPI 文档信息的任务，任务结果为获取到的 OpenAPI 文档信息</returns>
    internal static async Task<OpenApiDocumentDto> GetOpenApiDocument(string documentUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
        {
            throw new ArgumentException("OpenAPI 文档地址不能为空。", nameof(documentUrl));
        }

        if (!Uri.TryCreate(documentUrl, UriKind.Absolute, out Uri documentUri))
        {
            throw new ArgumentException("OpenAPI 文档地址必须是有效的绝对地址。", nameof(documentUrl));
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(documentUri, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            OpenApiDocumentDto result = JsonSerializer.Deserialize<OpenApiDocumentDto>(jsonContent, _openApiSerializerOptions);

            if (result == null)
            {
                throw new JsonException($"OpenAPI 文档“{documentUrl}”的内容为空或格式无效。");
            }

            result.Url = documentUrl;

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // 404，Swagger 文档不存在
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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
                console.WriteLine($"      读取 {documentUrl} 文档信息失败...");
                console.WriteLine($"      {ex}");
            });
        }

        return null;
    }
}
