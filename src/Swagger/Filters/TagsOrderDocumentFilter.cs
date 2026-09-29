// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.DynamicApplication;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Fast.Swagger;

/// <summary>
/// 标签文档排序/注释拦截器
/// </summary>
internal sealed class TagsOrderDocumentFilter : IDocumentFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        IEnumerable<OpenApiTag> orderedTags = DynamicApplicationContext.ControllerOrderCollection
            .Where(u => SwaggerDocumentBuilder.GetControllerGroups(u.Value.Item3).Any(c => c.Group == context.DocumentName))
            .OrderByDescending(u => u.Value.Item2)
            .ThenBy(u => u.Key)
            .Select(c => new OpenApiTag
            {
                Name = c.Value.Item1, Description = swaggerDoc.Tags?.FirstOrDefault(m => m.Name == c.Key)?.Description
            });

        // Microsoft.OpenAPI 2.x 的 Tags 属性使用集合类型
        swaggerDoc.Tags = new HashSet<OpenApiTag>(orderedTags);
    }
}
