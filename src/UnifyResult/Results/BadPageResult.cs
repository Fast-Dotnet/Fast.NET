// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fast.UnifyResult;

/// <summary>
/// 错误页面
/// </summary>
[SuppressSniffer]
public class BadPageResult : StatusCodeResult
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = "ModelState Invalid";

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; } = "User data verification failed. Please input it correctly.";

    /// <summary>
    /// 图标
    /// </summary>
    /// <remarks>必须是 base64 类型</remarks>
    public string Base64Icon { get; set; } =
        "data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIGZpbGw9Im5vbmUiIHhtbG5zPSJodHRwOi8vd3d3LnczLm9yZy8yMDAwL3N2ZyI+PHBhdGggZD0iTTE0LjIxIDEzLjVsMS43NjcgMS43NzMtLjcwNC43MDRMMTMuNSAxNC4yMWwtMS43NzMgMS43NzMtLjcwNC0uNzEgMS43NzQtMS43NzQtMS43NzQtMS43NzMuNzA0LS43MDQgMS43NzMgMS43NzQgMS43NzMtMS43NzQuNzA0LjcxMUwxNC4yMSAxMy41ek0yIDE1aDh2MUgxVjBoOC43MUwxNCA0LjI5VjEwaC0xVjVIOVYxSDJ2MTR6bTgtMTFoMi4yOUwxMCAxLjcxVjR6IiBmaWxsPSIjMTAxMDEwIi8+PC9zdmc+";

    /// <summary>
    /// 错误代码
    /// </summary>
    public string Code { get; set; } = "";

    /// <summary>
    /// 错误代码语言
    /// </summary>
    public string CodeLang { get; set; } = "json";

    /// <summary>
    /// 返回通用 401 错误页
    /// </summary>
    public static BadPageResult Status401Unauthorized =>
        new(StatusCodes.Status401Unauthorized)
        {
            Title = "401 Unauthorized", Code = "401 Unauthorized", Description = "", CodeLang = "txt"
        };

    /// <summary>
    /// 返回通用 403 错误页
    /// </summary>
    public static BadPageResult Status403Forbidden =>
        new(StatusCodes.Status403Forbidden) {Title = "403 Forbidden", Code = "403 Forbidden", Description = "", CodeLang = "txt"};

    /// <summary>
    /// 返回通用 404 错误页
    /// </summary>
    public static BadPageResult Status404NotFound =>
        new(StatusCodes.Status404NotFound) {Title = "404 Not Found", Code = "404 Not Found", Description = "", CodeLang = "txt"};

    /// <summary>
    /// 返回通用 500 错误页
    /// </summary>
    public static BadPageResult Status500InternalServerError =>
        new(StatusCodes.Status500InternalServerError)
        {
            Title = "500 Internal Server Error", Code = "500 Internal Server Error", Description = "", CodeLang = "txt"
        };

    /// <summary>
    /// 初始化默认返回 HTTP 400 的错误页结果
    /// </summary>
    public BadPageResult() : base(400)
    {
    }

    /// <summary>
    /// 使用指定 HTTP 状态码初始化错误页结果
    /// </summary>
    /// <param name="statusCode">要写入响应的 HTTP 状态码</param>
    public BadPageResult(int statusCode) : base(statusCode)
    {
    }

    /// <inheritdoc />
    public override void ExecuteResult(ActionContext context)
    {
        // 如果 Response 已经完成输出或 WebSocket 请求，则禁止写入
        if (context.HttpContext.WebSockets.IsWebSocketRequest || context.HttpContext.Response.HasStarted)
        {
            return;
        }

        base.ExecuteResult(context);

        context.HttpContext.Response.Body.Write(Encoding.UTF8.GetBytes(ToString()));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        // 获取当前类型信息
        Type thisType = typeof(BadPageResult);
        Assembly thisAssembly = thisType.Assembly;

        // 读取嵌入式页面路径
        string errorHtml =
            $"{thisAssembly.GetName().Name}{thisType.Namespace?.Replace(nameof(Fast), string.Empty)}.Assets.error.html";

        // 解析嵌入式文件流
        using Stream readStream = thisAssembly.GetManifestResourceStream(errorHtml);

        if (readStream != null)
        {
            // Stream.Read 不保证一次读满，使用 StreamReader 可避免页面被静默截断
            using var reader = new StreamReader(readStream, Encoding.UTF8, true);
            string content = reader.ReadToEnd();
            content = content.Replace($"@{{{nameof(Title)}}}", Title)
                .Replace($"@{{{nameof(Description)}}}", Description)
                .Replace($"@{{{nameof(StatusCode)}}}", StatusCode.ToString())
                .Replace($"@{{{nameof(Code)}}}", Code)
                .Replace($"@{{{nameof(CodeLang)}}}", CodeLang)
                .Replace($"@{{{nameof(Base64Icon)}}}", Base64Icon);

            return content;
        }

        throw new InvalidOperationException("The embedded resource file error.html could not be found");
    }
}
