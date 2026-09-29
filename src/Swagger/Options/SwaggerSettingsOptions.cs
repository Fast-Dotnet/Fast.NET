// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using Fast.Runtime;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Fast.Swagger;

/// <summary>
/// Swagger 配置选项
/// </summary>
[SuppressSniffer]
public sealed class SwaggerSettingsOptions : IPostConfigure
{
    /// <summary>
    /// 是否启用/注入规范化文档
    /// </summary>
    public bool? Enable { get; set; }

    /// <summary>
    /// 文档标题
    /// </summary>
    public string DocumentTitle { get; set; }

    /// <summary>
    /// 默认分组名
    /// </summary>
    public string DefaultGroupName { get; set; }

    /// <summary>
    /// 启用授权支持
    /// </summary>
    public bool? EnableAuthorized { get; set; }

    /// <summary>
    /// 格式化为 V2 版本
    /// </summary>
    public bool? FormatAsV2 { get; set; }

    /// <summary>
    /// 配置规范化文档地址
    /// </summary>
    public string RoutePrefix { get; set; }

    /// <summary>
    /// 文档展开设置
    /// </summary>
    public DocExpansion? DocExpansionState { get; set; }

    /// <summary>
    /// XML 描述文件
    /// </summary>
    public string[] XmlComments { get; set; }

    /// <summary>
    /// 分组信息
    /// </summary>
    public SwaggerOpenApiInfo[] GroupOpenApiInfos { get; set; }

    /// <summary>
    /// 安全定义
    /// </summary>
    public SwaggerOpenApiSecurityScheme[] SecurityDefinitions { get; set; }

    /// <summary>
    /// 配置 Servers
    /// </summary>
    public OpenApiServer[] Servers { get; set; }

    /// <summary>
    /// 隐藏 Servers
    /// </summary>
    public bool? HideServers { get; set; }

    /// <summary>
    /// 默认 swagger.json 路由模板
    /// </summary>
    public string RouteTemplate { get; set; }

    /// <summary>
    /// 配置安装第三方包的分组名
    /// </summary>
    public string[] PackagesGroups { get; set; }

    /// <summary>
    /// 启用枚举 Schema 筛选器
    /// </summary>
    public bool? EnableEnumSchemaFilter { get; set; }

    /// <summary>
    /// 启用标签排序筛选器
    /// </summary>
    public bool? EnableTagsOrderDocumentFilter { get; set; }

    /// <summary>
    /// 服务目录（修正 IIS 创建 Application 问题）
    /// </summary>
    public string ServerDir { get; set; }

    /// <summary>
    /// 配置规范化文档登录信息
    /// </summary>
    public SwaggerLoginInfo LoginInfo { get; set; }

    /// <summary>
    /// 启用 All Groups 功能
    /// </summary>
    public bool? EnableAllGroups { get; set; }

    /// <summary>
    /// 枚举类型生成值类型
    /// </summary>
    public bool? EnumToNumber { get; set; }

    /// <inheritdoc />
    public void PostConfigure()
    {
        Enable ??= true;

        DocumentTitle ??= "Specification Api Document";
        DefaultGroupName ??= "Default";
        FormatAsV2 ??= false;
        DocExpansionState ??= DocExpansion.List;

        // 加载项目注册和模块化/插件注释
        string frameworkPackageName = GetType().GetTypeInfo().Assembly.GetName().Name;
        IEnumerable<string> projectXmlComments = MAppContext.Assemblies.Where(u => u.GetName().Name != frameworkPackageName)
            .Select(t => t.GetName().Name);
        XmlComments ??= projectXmlComments.ToArray();

        GroupOpenApiInfos ??= new[] {new SwaggerOpenApiInfo {Group = DefaultGroupName}};

        EnableAuthorized ??= true;
        if (EnableAuthorized == true)
        {
            // Microsoft.OpenAPI 2.x 已移除 Reference，需求项在注册时通过方案标识创建引用
            SecurityDefinitions ??= new[]
            {
                new SwaggerOpenApiSecurityScheme
                {
                    Id = "Bearer",
                    Type = SecuritySchemeType.Http,
                    Name = "Authorization",
                    Description = "JWT Authorization header using the Bearer scheme.",
                    BearerFormat = "JWT",
                    Scheme = "bearer",
                    In = ParameterLocation.Header,
                    Requirement = new SwaggerOpenApiSecurityRequirementItem
                    {
                        Scheme = new OpenApiSecurityScheme(), Accesses = Array.Empty<string>()
                    }
                }
            };
        }

        Servers ??= Array.Empty<OpenApiServer>();
        HideServers ??= true;
        RouteTemplate ??= "swagger/{documentName}/swagger.json";
        PackagesGroups ??= Array.Empty<string>();
        EnableEnumSchemaFilter ??= true;
        EnableTagsOrderDocumentFilter ??= true;
        EnableAllGroups ??= false;
        EnumToNumber ??= false;
    }
}
