// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Fast.DynamicApplication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Fast.Swagger;

/// <summary>
/// 规范化文档构建器
/// </summary>
[SuppressSniffer]
public static class SwaggerDocumentBuilder
{
    /// <summary>
    /// 所有分组默认的组名 Key
    /// </summary>
    private const string AllGroupsKey = "All Groups";

    /// <summary>
    /// 分组信息
    /// </summary>
    private static readonly IEnumerable<GroupExtraInfo> DocumentGroupExtras;

    /// <summary>
    /// 带排序的分组名
    /// </summary>
    private static readonly Regex _groupOrderRegex;

    /// <summary>
    /// 文档分组列表
    /// </summary>
    public static readonly IEnumerable<string> DocumentGroups;

    /// <summary>
    /// 初始化静态数据
    /// </summary>
    static SwaggerDocumentBuilder()
    {
        // 初始化常量
        _groupOrderRegex = new Regex(@"@(?<order>[0-9]+$)");
        GetActionGroupsCached = new ConcurrentDictionary<MethodInfo, IEnumerable<GroupExtraInfo>>();
        GetControllerGroupsCached = new ConcurrentDictionary<Type, IEnumerable<GroupExtraInfo>>();
        GetGroupOpenApiInfoCached = new ConcurrentDictionary<string, SwaggerOpenApiInfo>();
        GetControllerTagCached = new ConcurrentDictionary<ControllerActionDescriptor, string>();
        GetActionTagCached = new ConcurrentDictionary<ApiDescription, string>();

        // 默认分组，支持多个逗号分割
        DocumentGroupExtras = new List<GroupExtraInfo> {ResolveGroupExtraInfo(Penetrates.SwaggerSettings.DefaultGroupName)};

        // 加载所有分组
        DocumentGroups = ReadGroups();
    }

    /// <summary>
    /// 检查方法是否在分组中
    /// </summary>
    /// <param name="currentGroup">当前正在处理的文档分组</param>
    /// <param name="apiDescription">当前 API 的描述信息</param>
    /// <returns>API 应包含在当前文档分组中时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool CheckApiDescriptionInCurrentGroup(string currentGroup, ApiDescription apiDescription)
    {
        if (!apiDescription.TryGetMethodInfo(out MethodInfo method))
        {
            return false;
        }

        // 处理 Mvc 和 WebAPI 混合项目路由问题
        if (typeof(Controller).IsAssignableFrom(method.DeclaringType)
            && apiDescription.ActionDescriptor.ActionConstraints == null)
        {
            return false;
        }

        // 处理贴有 [ApiExplorerSettings(IgnoreApi = true)] 或者 [ApiDescriptionSettings(false)] 特性的接口
        ApiExplorerSettingsAttribute apiExplorerSettings = method.GetFoundAttribute<ApiExplorerSettingsAttribute>(true);
        if (apiExplorerSettings?.IgnoreApi == true)
        {
            return false;
        }

        if (currentGroup == AllGroupsKey)
        {
            return true;
        }

        return GetActionGroups(method).Any(u => u.Group == currentGroup);
    }

    /// <summary>
    /// 获取所有的规范化分组信息
    /// </summary>
    /// <returns>获取到的所有的规范化分组信息集合</returns>
    public static List<SwaggerOpenApiInfo> GetOpenApiGroups()
    {
        var openApiGroups = new List<SwaggerOpenApiInfo>();
        foreach (string group in DocumentGroups)
        {
            openApiGroups.Add(GetGroupOpenApiInfo(group));
        }

        return openApiGroups;
    }

    /// <summary>
    /// 获取分组信息缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<string, SwaggerOpenApiInfo> GetGroupOpenApiInfoCached;

    /// <summary>
    /// 获取分组配置信息
    /// </summary>
    /// <param name="group">文档分组名称</param>
    /// <returns>获取到的分组配置信息</returns>
    public static SwaggerOpenApiInfo GetGroupOpenApiInfo(string group)
    {
        return GetGroupOpenApiInfoCached.GetOrAdd(group, Function);

        static SwaggerOpenApiInfo Function(string group)
        {
            // 替换路由模板
            string routeTemplate =
                Penetrates.SwaggerSettings.RouteTemplate.Replace("{documentName}", Uri.EscapeDataString(group));
            if (!string.IsNullOrWhiteSpace(Penetrates.SwaggerSettings.ServerDir))
            {
                routeTemplate = Penetrates.SwaggerSettings.ServerDir + "/" + routeTemplate;
            }

            string template = $"/{routeTemplate}";

            SwaggerOpenApiInfo groupInfo = Penetrates.SwaggerSettings.GroupOpenApiInfos.FirstOrDefault(u => u.Group == group);
            if (groupInfo != null)
            {
                groupInfo.RouteTemplate = template;
                groupInfo.Title ??= group;
            }
            else
            {
                groupInfo = new SwaggerOpenApiInfo {Group = group, RouteTemplate = template};
            }

            return groupInfo;
        }
    }

    /// <summary>
    /// 构建 Swagger 全局配置
    /// </summary>
    /// <param name="swaggerOptions">Swagger 全局配置</param>
    /// <param name="configure">额外的 Swagger 配置操作</param>
    internal static void Build(SwaggerOptions swaggerOptions, Action<SwaggerOptions> configure = null)
    {
        // 配置生成的 OpenAPI 规范版本
        swaggerOptions.OpenApiVersion = Penetrates.SwaggerSettings.FormatAsV2 == true
            ? OpenApiSpecVersion.OpenApi2_0
            : OpenApiSpecVersion.OpenApi3_0;

        // 判断是否启用 Server
        if (Penetrates.SwaggerSettings.HideServers != true)
        {
            // 启动服务器 Servers
            swaggerOptions.PreSerializeFilters.Add((swagger, request) =>
            {
                // 默认 Server
                var servers = new List<OpenApiServer>
                {
                    new() {Url = $"{request.Scheme}://{request.Host.Value}", Description = "Default"}
                };
                servers.AddRange(Penetrates.SwaggerSettings.Servers);

                swagger.Servers = servers;
            });
        }

        // 配置路由模板
        swaggerOptions.RouteTemplate = Penetrates.SwaggerSettings.RouteTemplate;

        configure?.Invoke(swaggerOptions);
    }

    /// <summary>
    /// Swagger 生成器构建
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器配置</param>
    /// <param name="configure">自定义配置</param>
    internal static void BuildGen(SwaggerGenOptions swaggerGenOptions, Action<SwaggerGenOptions> configure = null)
    {
        // 创建分组文档
        CreateSwaggerDocs(swaggerGenOptions);

        // 加载分组控制器和动作方法列表
        LoadGroupControllerWithActions(swaggerGenOptions);

        // 配置 Swagger OperationIds
        ConfigureOperationIds(swaggerGenOptions);

        // 配置 Swagger SchemaId
        ConfigureSchemaIds(swaggerGenOptions);

        // 配置标签
        ConfigureTagsAction(swaggerGenOptions);

        // 配置 Action 排序
        ConfigureActionSequence(swaggerGenOptions);

        // 加载注释描述文件
        LoadXmlComments(swaggerGenOptions);

        // 配置授权
        ConfigureSecurities(swaggerGenOptions);

        //使得 Swagger 能够正确地显示 Enum 的对应关系
        if (Penetrates.SwaggerSettings.EnableEnumSchemaFilter == true)
        {
            swaggerGenOptions.SchemaFilter<EnumSchemaFilter>();
        }

        // 修复 editor.swagger.io 生成不能正常处理 C# object 类型问题
        swaggerGenOptions.SchemaFilter<AnySchemaFilter>();

        // 支持控制器排序操作
        if (Penetrates.SwaggerSettings.EnableTagsOrderDocumentFilter == true)
        {
            swaggerGenOptions.DocumentFilter<TagsOrderDocumentFilter>();
        }

        // 添加 Action 操作过滤器
        swaggerGenOptions.OperationFilter<ApiActionFilter>();

        configure?.Invoke(swaggerGenOptions);
    }

    /// <summary>
    /// Swagger UI 构建
    /// </summary>
    /// <param name="swaggerUIOptions">Swagger UI 配置选项</param>
    /// <param name="configure">Swagger UI 配置操作</param>
    internal static void BuildUI(SwaggerUIOptions swaggerUIOptions, Action<SwaggerUIOptions> configure = null)
    {
        // 配置分组终点路由
        CreateGroupEndpoint(swaggerUIOptions);

        // 配置文档标题
        swaggerUIOptions.DocumentTitle = Penetrates.SwaggerSettings.DocumentTitle;

        // 配置 UI 地址（处理二级虚拟目录）
        swaggerUIOptions.RoutePrefix = Penetrates.SwaggerSettings.RoutePrefix ?? string.Empty;

        // 文档展开设置
        swaggerUIOptions.DocExpansion(Penetrates.SwaggerSettings.DocExpansionState!.Value);

        // 自定义 Swagger 首页
        CustomizeIndex(swaggerUIOptions);

        // 配置多语言错误响应和自动登录令牌处理
        AddDefaultInterceptor(swaggerUIOptions);

        configure?.Invoke(swaggerUIOptions);
    }

    /// <summary>
    /// 创建分组文档
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器对象</param>
    private static void CreateSwaggerDocs(SwaggerGenOptions swaggerGenOptions)
    {
        foreach (string group in DocumentGroups)
        {
            var groupOpenApiInfo = GetGroupOpenApiInfo(group) as OpenApiInfo;
            swaggerGenOptions.SwaggerDoc(group, groupOpenApiInfo);
        }
    }

    /// <summary>
    /// 加载分组控制器和动作方法列表
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器配置</param>
    private static void LoadGroupControllerWithActions(SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.DocInclusionPredicate(CheckApiDescriptionInCurrentGroup);
    }

    /// <summary>
    /// 配置标签
    /// </summary>
    /// <param name="swaggerGenOptions">swagger Gen Options 配置</param>
    private static void ConfigureTagsAction(SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.TagActionsBy(apiDescription => { return new[] {GetActionTag(apiDescription)}; });
    }

    /// <summary>
    /// 配置 Action 排序
    /// </summary>
    /// <param name="swaggerGenOptions">swagger Gen Options 配置</param>
    private static void ConfigureActionSequence(SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.OrderActionsBy(apiDesc =>
        {
            ApiDescriptionSettingsAttribute apiDescriptionSettings =
                apiDesc.CustomAttributes().FirstOrDefault(u => u is ApiDescriptionSettingsAttribute) as
                    ApiDescriptionSettingsAttribute
                ?? new ApiDescriptionSettingsAttribute();

            return (int.MaxValue - apiDescriptionSettings.Order).ToString().PadLeft(int.MaxValue.ToString().Length, '0');
        });
    }

    /// <summary>
    /// 配置 Swagger OperationIds
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器配置</param>
    private static void ConfigureOperationIds(SwaggerGenOptions swaggerGenOptions)
    {
        swaggerGenOptions.CustomOperationIds(apiDescription =>
        {
            bool isMethod = apiDescription.TryGetMethodInfo(out MethodInfo method);

            // 判断是否自定义了 [OperationId] 特性
            if (isMethod && method.IsDefined(typeof(OperationIdAttribute), false))
            {
                return method.GetCustomAttribute<OperationIdAttribute>(false)?.OperationId;
            }

            string operationId = apiDescription.RelativePath?.Replace("/", "-").Replace("{", "-").Replace("}", "-")
                                 + "-"
                                 + apiDescription.HttpMethod?.ToLower().FirstCharToUpper();

            return operationId.Replace("--", "-");
        });
    }

    /// <summary>
    /// 配置 Swagger SchemaIds
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器配置</param>
    private static void ConfigureSchemaIds(SwaggerGenOptions swaggerGenOptions)
    {
        static string DefaultSchemaIdSelector(Type modelType)
        {
            string modelName = modelType.Name;

            // 处理泛型类型问题
            if (modelType.IsConstructedGenericType)
            {
                string prefix = modelType.GetGenericArguments()
                    .Select(DefaultSchemaIdSelector)
                    .Aggregate((previous, current) => previous + current);

                // 通过 _ 拼接多个泛型
                modelName = modelName.Split('`').First() + "_" + prefix;
            }

            // 判断是否自定义了 [SchemaId] 特性，解决模块化多个程序集命名冲突
            bool isCustomize = modelType.IsDefined(typeof(SchemaIdAttribute));
            if (isCustomize)
            {
                SchemaIdAttribute schemaIdAttribute = modelType.GetCustomAttribute<SchemaIdAttribute>();
                if (schemaIdAttribute is {Replace: false})
                {
                    return schemaIdAttribute.SchemaId + modelName;
                }

                return schemaIdAttribute?.SchemaId;
            }

            return modelName;
        }

        swaggerGenOptions.CustomSchemaIds(DefaultSchemaIdSelector);
    }

    /// <summary>
    /// 加载注释描述文件
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器配置</param>
    private static void LoadXmlComments(SwaggerGenOptions swaggerGenOptions)
    {
        string[] xmlComments = Penetrates.SwaggerSettings.XmlComments;
        var members = new Dictionary<string, XElement>();

        // 显式继承的注释
        var regex = new Regex(@"[A-Z]:[a-zA-Z_@\.]+");
        // 隐式继承的注释
        var regex2 = new Regex(@"[A-Z]:[a-zA-Z_@\.]+\.");

        // 支持注释完整特性，包括 inheritdoc 注释语法
        foreach (string xmlComment in xmlComments)
        {
            string assemblyXmlName = xmlComment.EndsWith(".xml") ? xmlComment : $"{xmlComment}.xml";
            string assemblyXmlPath = Path.Combine(AppContext.BaseDirectory, assemblyXmlName);

            if (File.Exists(assemblyXmlPath))
            {
                var xmlDoc = XDocument.Load(assemblyXmlPath);

                // 查找所有 member[name] 节点，且不包含 <inheritdoc /> 和 <exclude /> 节点的注释
                IEnumerable<XElement> memberNotInheritdocElementList =
                    xmlDoc.XPathSelectElements("/doc/members/member[@name and not(inheritdoc) and not(exclude)]");

                foreach (XElement memberElement in memberNotInheritdocElementList)
                {
                    members.TryAdd(memberElement.Attribute("name")!.Value, memberElement);
                }

                // 查找所有 member[name] 含有 <inheritdoc /> 节点的注释
                IEnumerable<XElement> memberElementList = xmlDoc.XPathSelectElements("/doc/members/member[inheritdoc]");
                foreach (XElement memberElement in memberElementList)
                {
                    XElement inheritdocElement = memberElement.Element("inheritdoc");
                    XAttribute cref = inheritdocElement!.Attribute("cref");
                    string value = cref?.Value;

                    // 处理不带 cref 的 inheritdoc 注释
                    if (value == null)
                    {
                        string memberName = inheritdocElement.Parent!.Attribute("name")!.Value;

                        // 处理隐式实现接口的注释
                        // 注释格式：M:Fast.NET.Application.TestInheritdoc.Fast.NET#Application#ITestInheritdoc#Abc(System.String)
                        // 匹配格式：[A-Z]:[a-zA-Z_@\.]+\
                        // 处理逻辑：直接替换匹配为空，然后讲 # 替换为 . 查找即可
                        if (memberName.Contains('#'))
                        {
                            value = $"{memberName[..2]}{regex2.Replace(memberName, "").Replace('#', '.')}";
                        }
                        // 处理带参数的注释
                        // 注释格式：M:Fast.NET.Application.TestInheritdoc.WithParams(System.String)
                        // 匹配格式：[A-Z]:[a-zA-Z_@\.]+
                        // 处理逻辑：匹配出不带参数的部分，然后获取类型命名空间，最后调用 GenerateInheritdocCref 进行生成
                        else if (memberName.Contains('('))
                        {
                            string noParamsClassName = regex.Match(memberName).Value;
                            string className =
                                noParamsClassName[
                                    noParamsClassName.IndexOf(":", StringComparison.Ordinal)..noParamsClassName.LastIndexOf(".",
                                        StringComparison.Ordinal)];
                            value = GenerateInheritdocCref(xmlDoc, memberName, className);
                        }
                        // 处理不带参数的注释
                        // 注释格式：M:Fast.NET.Application.TestInheritdoc.WithParams
                        // 匹配格式：无
                        // 处理逻辑：获取类型命名空间，最后调用 GenerateInheritdocCref 进行生成
                        else
                        {
                            string className =
                                memberName[
                                    memberName.IndexOf(":", StringComparison.Ordinal)..memberName.LastIndexOf(".",
                                        StringComparison.Ordinal)];
                            value = GenerateInheritdocCref(xmlDoc, memberName, className);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    // 处理带 cref 的 inheritdoc 注释
                    if (members.TryGetValue(value, out XElement realDocMember))
                    {
                        memberElement.SetAttributeValue("_ref_", value);
                        inheritdocElement.Parent!.ReplaceNodes(realDocMember.Nodes());
                    }
                }

                swaggerGenOptions.IncludeXmlComments(() => new XPathDocument(xmlDoc.CreateReader()), true);
            }
        }
    }

    /// <summary>
    /// 生成 Inheritdoc cref 属性
    /// </summary>
    /// <param name="xmlDoc">用于生成继承引用的 XML 文档节点</param>
    /// <param name="memberName">XML 文档成员名称</param>
    /// <param name="className">声明该成员的类型名称</param>
    /// <returns>生成的 Inheritdoc cref 属性</returns>
    private static string GenerateInheritdocCref(XDocument xmlDoc, string memberName, string className)
    {
        XElement classElement = xmlDoc.XPathSelectElements($"/doc/members/member[@name='{"T" + className}' and @_ref_]")
            .FirstOrDefault();
        if (classElement == null)
        {
            return null;
        }

        string _ref_value = classElement.Attribute("_ref_")?.Value;
        if (_ref_value == null)
        {
            return null;
        }

        string classCrefValue = _ref_value[_ref_value.IndexOf(":", StringComparison.Ordinal)..];
        return memberName.Replace(className, classCrefValue);
    }

    /// <summary>
    /// 配置授权
    /// </summary>
    /// <param name="swaggerGenOptions">Swagger 生成器配置</param>
    private static void ConfigureSecurities(SwaggerGenOptions swaggerGenOptions)
    {
        // 判断是否启用了授权
        if (Penetrates.SwaggerSettings.EnableAuthorized != true || Penetrates.SwaggerSettings.SecurityDefinitions.Length == 0)
        {
            return;
        }

        var openApiSecurityRequirement = new OpenApiSecurityRequirement();

        // 生成安全定义
        foreach (SwaggerOpenApiSecurityScheme securityDefinition in Penetrates.SwaggerSettings.SecurityDefinitions)
        {
            // 必须定义Id
            if (string.IsNullOrWhiteSpace(securityDefinition.Id))
            {
                continue;
            }

            // 添加安全定义
            var openApiSecurityScheme = securityDefinition as OpenApiSecurityScheme;
            swaggerGenOptions.AddSecurityDefinition(securityDefinition.Id, openApiSecurityScheme);

            SwaggerOpenApiSecurityRequirementItem securityRequirement = securityDefinition.Requirement;

            // Microsoft.OpenAPI 2.x 通过方案标识创建安全方案引用
            if (securityRequirement?.Scheme is not null)
            {
                var schemeReference = new OpenApiSecuritySchemeReference(securityDefinition.Id);
                openApiSecurityRequirement.Add(schemeReference, securityRequirement.Accesses.ToList());
            }
        }

        if (openApiSecurityRequirement.Count > 0)
        {
            // Swashbuckle 10 通过文档回调注册安全需求
            swaggerGenOptions.AddSecurityRequirement(_ => openApiSecurityRequirement);
        }
    }

    /// <summary>
    /// 配置分组终点路由
    /// </summary>
    /// <param name="swaggerUIOptions">Swagger UI 配置选项</param>
    private static void CreateGroupEndpoint(SwaggerUIOptions swaggerUIOptions)
    {
        foreach (string group in DocumentGroups)
        {
            SwaggerOpenApiInfo groupOpenApiInfo = GetGroupOpenApiInfo(group);

            swaggerUIOptions.SwaggerEndpoint(groupOpenApiInfo.RouteTemplate, groupOpenApiInfo.Title ?? group);
        }
    }

    /// <summary>
    /// 自定义 Swagger 首页
    /// </summary>
    /// <param name="swaggerUIOptions">Swagger UI 配置选项</param>
    private static void CustomizeIndex(SwaggerUIOptions swaggerUIOptions)
    {
        Type thisType = typeof(SwaggerDocumentBuilder);
        Assembly thisAssembly = thisType.Assembly;

        string customIndex = $"{thisAssembly.GetName().Name}.Assets.index.html";
        swaggerUIOptions.IndexStream = () =>
        {
            StringBuilder htmlBuilder;

            // 读取文件内容
            using (Stream stream = thisAssembly.GetManifestResourceStream(customIndex))
            {
                using var reader = new StreamReader(stream);
                htmlBuilder = new StringBuilder(reader.ReadToEnd());
            }

            // 返回新的内存流
            byte[] byteArray = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            return new MemoryStream(byteArray);
        };

        // 添加登录信息配置
        SwaggerLoginInfo additional = Penetrates.SwaggerSettings.LoginInfo;
        if (additional != null)
        {
            swaggerUIOptions.ConfigObject.AdditionalItems.Add(nameof(Penetrates.SwaggerSettings.LoginInfo), additional);
        }
    }

    /// <summary>
    /// 添加默认请求/响应拦截器
    /// </summary>
    /// <param name="swaggerUIOptions">Swagger UI 配置选项</param>
    private static void AddDefaultInterceptor(SwaggerUIOptions swaggerUIOptions)
    {
        // 配置多语言错误响应和自动登录令牌处理
        swaggerUIOptions.UseRequestInterceptor("function(request) { return defaultRequestInterceptor(request); }");
        swaggerUIOptions.UseResponseInterceptor("function(response) { return defaultResponseInterceptor(response); }");
    }

    /// <summary>
    /// 读取所有分组信息
    /// </summary>
    /// <returns>读取到的所有分组信息集合</returns>
    private static IEnumerable<string> ReadGroups()
    {
        // 获取所有的控制器和动作方法
        var controllers = MAppContext.EffectiveTypes.Where(DynamicApplicationContext.IsApiController).ToList();
        if (!controllers.Any())
        {
            var defaultGroups = new List<string> {Penetrates.SwaggerSettings.DefaultGroupName};

            if (Penetrates.SwaggerSettings.EnableAllGroups == true)
            {
                defaultGroups.Add(AllGroupsKey);
            }

            return defaultGroups;
        }

        IEnumerable<MethodInfo> actions = controllers.SelectMany(c => c.GetMethods().Where(u => IsApiAction(u, c)));

        // 合并所有分组
        IEnumerable<GroupExtraInfo> groupOrders = controllers.SelectMany(GetControllerGroups)
            .Union(actions.SelectMany(GetActionGroups))
            .Where(u => u is {Visible: true})
            // 分组后取最大排序
            .GroupBy(u => u.Group)
            .Select(u => new GroupExtraInfo {Group = u.Key, Order = u.Max(x => x.Order), Visible = true});

        // 分组排序
        IEnumerable<string> groups = groupOrders.OrderByDescending(u => u.Order)
            .ThenBy(u => u.Group)
            .Select(u => u.Group)
            .Union(Penetrates.SwaggerSettings.PackagesGroups);

        if (Penetrates.SwaggerSettings.EnableAllGroups == true)
        {
            groups = groups.Concat(new[] {AllGroupsKey});
        }

        return groups;
    }

    /// <summary>
    /// 获取控制器组缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<Type, IEnumerable<GroupExtraInfo>> GetControllerGroupsCached;

    /// <summary>
    /// 获取控制器分组列表
    /// </summary>
    /// <param name="type">目标类型</param>
    /// <returns>获取到的控制器分组列表集合</returns>
    public static IEnumerable<GroupExtraInfo> GetControllerGroups(Type type)
    {
        return GetControllerGroupsCached.GetOrAdd(type, Function);

        static IEnumerable<GroupExtraInfo> Function(Type type)
        {
            // 如果控制器没有定义 [ApiDescriptionSettings] 特性，则返回默认分组
            if (!type.IsDefined(typeof(ApiDescriptionSettingsAttribute), true))
            {
                return DocumentGroupExtras;
            }

            ApiDescriptionSettingsAttribute apiDescriptionSettings =
                type.GetCustomAttribute<ApiDescriptionSettingsAttribute>(true);
            if (apiDescriptionSettings?.Groups == null || apiDescriptionSettings.Groups.Length == 0)
            {
                return DocumentGroupExtras;
            }

            // 处理分组额外信息
            var groupExtras = new List<GroupExtraInfo>();
            foreach (string group in apiDescriptionSettings.Groups)
            {
                groupExtras.Add(ResolveGroupExtraInfo(group));
            }

            return groupExtras;
        }
    }

    /// <summary>
    /// <see cref="GetActionGroups(MethodInfo)"/> 缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<MethodInfo, IEnumerable<GroupExtraInfo>> GetActionGroupsCached;

    /// <summary>
    /// 获取动作方法分组列表
    /// </summary>
    /// <param name="method">目标方法</param>
    /// <returns>获取到的动作方法分组列表集合</returns>
    public static IEnumerable<GroupExtraInfo> GetActionGroups(MethodInfo method)
    {
        return GetActionGroupsCached.GetOrAdd(method, Function);

        static IEnumerable<GroupExtraInfo> Function(MethodInfo method)
        {
            // 如果动作方法没有定义 [ApiDescriptionSettings] 特性，则返回所在控制器分组
            if (!method.IsDefined(typeof(ApiDescriptionSettingsAttribute), true))
            {
                return GetControllerGroups(method.ReflectedType);
            }

            ApiDescriptionSettingsAttribute apiDescriptionSettings =
                method.GetCustomAttribute<ApiDescriptionSettingsAttribute>(true);
            if (apiDescriptionSettings?.Groups == null || apiDescriptionSettings.Groups.Length == 0)
            {
                return GetControllerGroups(method.ReflectedType);
            }

            // 处理排序
            var groupExtras = new List<GroupExtraInfo>();
            foreach (string group in apiDescriptionSettings.Groups)
            {
                groupExtras.Add(ResolveGroupExtraInfo(group));
            }

            return groupExtras;
        }
    }

    /// <summary>
    /// <see cref="GetActionTag(ApiDescription)"/> 缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<ControllerActionDescriptor, string> GetControllerTagCached;

    /// <summary>
    /// 获取控制器标签
    /// </summary>
    /// <param name="controllerActionDescriptor">当前控制器操作的描述信息</param>
    /// <returns>获取到的控制器标签</returns>
    public static string GetControllerTag(ControllerActionDescriptor controllerActionDescriptor)
    {
        return GetControllerTagCached.GetOrAdd(controllerActionDescriptor, Function);

        static string Function(ControllerActionDescriptor controllerActionDescriptor)
        {
            TypeInfo type = controllerActionDescriptor.ControllerTypeInfo;
            // 如果动作方法没有定义 [ApiDescriptionSettings] 特性，则返回所在控制器名
            if (!type.IsDefined(typeof(ApiDescriptionSettingsAttribute), true))
            {
                return controllerActionDescriptor.ControllerName;
            }

            return controllerActionDescriptor.ControllerName;
        }
    }

    /// <summary>
    /// <see cref="GetActionTag(ApiDescription)"/> 缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<ApiDescription, string> GetActionTagCached;

    /// <summary>
    /// 获取动作方法标签
    /// </summary>
    /// <param name="apiDescription">当前 API 的描述信息</param>
    /// <returns>获取到的动作方法标签</returns>
    public static string GetActionTag(ApiDescription apiDescription)
    {
        return GetActionTagCached.GetOrAdd(apiDescription, Function);

        static string Function(ApiDescription apiDescription)
        {
            if (!apiDescription.TryGetMethodInfo(out MethodInfo method)
                || apiDescription.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
            {
                return Assembly.GetEntryAssembly()?.GetName().Name;
            }

            // 如果动作方法没有定义 [ApiDescriptionSettings] 特性，则返回所在控制器名
            if (!method.IsDefined(typeof(ApiDescriptionSettingsAttribute), true))
            {
                return GetControllerTag(controllerActionDescriptor);
            }

            return GetControllerTag(controllerActionDescriptor);
        }
    }

    /// <summary>
    /// 是否是动作方法
    /// </summary>
    /// <param name="method">目标方法</param>
    /// <param name="ReflectedType">Reflected 类型</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsApiAction(MethodInfo method, Type ReflectedType)
    {
        // 不是非公开、抽象、静态、泛型方法
        if (!method.IsPublic || method.IsAbstract || method.IsStatic || method.IsGenericMethod)
        {
            return false;
        }

        // 如果所在类型不是控制器，则该行为也被忽略
        if (method.ReflectedType != ReflectedType || method.DeclaringType == typeof(object))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 解析分组附加信息
    /// </summary>
    /// <param name="group">分组名</param>
    /// <returns>解析后的分组附加信息</returns>
    private static GroupExtraInfo ResolveGroupExtraInfo(string group)
    {
        string realGroup;
        int order = 0;

        if (!_groupOrderRegex.IsMatch(group))
        {
            realGroup = group;
        }
        else
        {
            realGroup = _groupOrderRegex.Replace(group, "");
            order = int.Parse(_groupOrderRegex.Match(group).Groups["order"].Value);
        }

        SwaggerOpenApiInfo groupOpenApiInfo = GetGroupOpenApiInfo(realGroup);
        return new GroupExtraInfo
        {
            Group = realGroup, Order = groupOpenApiInfo.Order ?? order, Visible = groupOpenApiInfo.Visible ?? true
        };
    }
}
