// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Fast.Runtime;

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI 配置
/// </summary>
[SuppressSniffer]
public class OpenApiSettingsOptions : IPostConfigure
{
    /// <summary>
    /// 文件夹分组
    /// </summary>
    public bool? FolderGroup { get; set; }

    /// <summary>
    /// 导入声明映射
    /// </summary>
    /// <remarks>导出也只会会忽略</remarks>
    public List<OpenApiImportSchemaMappingSettingsOptions> ImportSchemaMappings { get; set; }

    /// <summary>
    /// 导入类型声明
    /// </summary>
    public List<OpenApiImportTypeMappingSettingsOptions> ImportTypeMappings { get; set; }

    /// <summary>
    /// 忽略声明
    /// </summary>
    public HashSet<string> IgnoreSchemas { get; set; }

    /// <summary>
    /// 分页声明属性
    /// </summary>
    public HashSet<string> PagedSchemaProperties { get; set; }

    /// <summary>
    /// 基础类型映射
    /// </summary>
    public IDictionary<string, string> BaseTypeMappings { get; set; }

    /// <inheritdoc />
    public void PostConfigure()
    {
        FolderGroup ??= true;
        ImportSchemaMappings ??=
        [
            new OpenApiImportSchemaMappingSettingsOptions
            {
                Name = "ElSelectorOutput", WebImportPath = "fast-element-plus", MobileImportPath = ""
            },
            new OpenApiImportSchemaMappingSettingsOptions
            {
                Name = "ElTreeOutput", WebImportPath = "fast-element-plus", MobileImportPath = ""
            },
            new OpenApiImportSchemaMappingSettingsOptions
            {
                Name = "FaTableEnumColumnCtx", WebImportPath = "fast-element-plus", MobileImportPath = ""
            },
            new OpenApiImportSchemaMappingSettingsOptions
            {
                Name = "PagedInput", WebImportPath = "fast-element-plus", MobileImportPath = ""
            },
            new OpenApiImportSchemaMappingSettingsOptions
            {
                Name = "PagedResult", WebImportPath = "fast-element-plus", MobileImportPath = ""
            }
        ];
        ImportTypeMappings ??=
        [
            new OpenApiImportTypeMappingSettingsOptions {Name = "RestfulResult_", MappingName = "{0}"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "IActionResult", MappingName = ""},
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "PagedResult_List_ElSelectorOutput_",
                RefSchema = ["PagedResult", "ElSelectorOutput"],
                MappingName = "PagedResult<ElSelectorOutput<{0}>[]>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "PagedResult_List_ElTreeOutput_",
                RefSchema = ["PagedResult", "ElTreeOutput"],
                MappingName = "PagedResult<ElTreeOutput<{0}>[]>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "PagedResult_ElSelectorOutput_",
                RefSchema = ["PagedResult", "ElSelectorOutput"],
                MappingName = "PagedResult<ElSelectorOutput<{0}>>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "PagedResult_ElTreeOutput_",
                RefSchema = ["PagedResult", "ElTreeOutput"],
                MappingName = "PagedResult<ElTreeOutput<{0}>>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "PagedResult_List_", RefSchema = ["PagedResult"], MappingName = "PagedResult<{0}>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "PagedResult_", RefSchema = ["PagedResult"], MappingName = "PagedResult<{0}>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_ElSelectorOutput_", RefSchema = ["ElSelectorOutput"], MappingName = "ElSelectorOutput<{0}>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_ElTreeOutput_", RefSchema = ["ElTreeOutput"], MappingName = "ElTreeOutput<{0}>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "ElSelectorOutput_", RefSchema = ["ElSelectorOutput"], MappingName = "ElSelectorOutput<{0}>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "ElTreeOutput_", RefSchema = ["ElTreeOutput"], MappingName = "ElTreeOutput<{0}>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_IDictionary_StringInt64", MappingName = "Record<string, string>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_Dictionary_StringInt64", MappingName = "Record<string, string>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_IDictionary_StringInt32", MappingName = "Record<string, number>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_Dictionary_StringInt32", MappingName = "Record<string, number>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_IDictionary_StringObject", MappingName = "Record<string, unknown>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_Dictionary_StringObject", MappingName = "Record<string, unknown>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_IDictionary_StringString", MappingName = "Record<string, string>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "List_Dictionary_StringString", MappingName = "Record<string, string>[]"
            },
            new OpenApiImportTypeMappingSettingsOptions {Name = "List_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "IDictionary_StringInt64", MappingName = "Record<string, string>"
            },
            new OpenApiImportTypeMappingSettingsOptions {Name = "Dictionary_StringInt64", MappingName = "Record<string, string>"},
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "IDictionary_StringInt32", MappingName = "Record<string, number>"
            },
            new OpenApiImportTypeMappingSettingsOptions {Name = "Dictionary_StringInt32", MappingName = "Record<string, number>"},
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "IDictionary_StringObject", MappingName = "Record<string, unknown>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "Dictionary_StringObject", MappingName = "Record<string, unknown>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "IDictionary_StringString", MappingName = "Record<string, string>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "Dictionary_StringString", MappingName = "Record<string, string>"
            },
            new OpenApiImportTypeMappingSettingsOptions {Name = "IDictionary_StringList_", MappingName = "Record<string, {0}[]>"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "Dictionary_StringList_", MappingName = "Record<string, {0}[]>"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "IDictionary_String", MappingName = "Record<string, {0}>"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "Dictionary_String", MappingName = "Record<string, {0}>"},
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "IReadOnlyDictionary_String", MappingName = "Readonly<Record<string, {0}>>"
            },
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "ReadOnlyDictionary_String", MappingName = "Readonly<Record<string, {0}>>"
            },
            new OpenApiImportTypeMappingSettingsOptions {Name = "SortedDictionary_String", MappingName = "Record<string, {0}>"},
            new OpenApiImportTypeMappingSettingsOptions
            {
                Name = "ConcurrentDictionary_String", MappingName = "Record<string, {0}>"
            },
            new OpenApiImportTypeMappingSettingsOptions {Name = "IEnumerable_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "ICollection_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "IReadOnlyCollection_", MappingName = "readonly {0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "IReadOnlyList_", MappingName = "readonly {0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "Collection_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "ObservableCollection_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "HashSet_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "ISet_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "Queue_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "Stack_", MappingName = "{0}[]"},
            new OpenApiImportTypeMappingSettingsOptions {Name = "Nullable_", MappingName = "{0}"}
        ];
        IgnoreSchemas ??= [];
        PagedSchemaProperties ??=
        [
            "pageIndex",
            "pageSize",
            "searchValue",
            "searchTimeList",
            "searchList",
            "sortList",
            "enablePaged",
            "isOrderBy"
        ];
        BaseTypeMappings ??= new Dictionary<string, string>
        {
            {"List_Nullable_Int64", "string[]"},
            {"List_Nullable_Int32", "number[]"},
            {"List_Nullable_Decimal", "number[]"},
            {"List_Nullable_Double", "number[]"},
            {"List_Nullable_Float", "number[]"},
            {"List_Nullable_Integer", "number[]"},
            {"List_Nullable_Boolean", "boolean[]"},
            {"List_Nullable_String", "string[]"},
            {"Nullable_Int64", "string"},
            {"Nullable_Int32", "number"},
            {"Nullable_Decimal", "number"},
            {"Nullable_Double", "number"},
            {"Nullable_Float", "number"},
            {"Nullable_Integer", "number"},
            {"Nullable_Boolean", "boolean"},
            {"Nullable_String", "string"},
            {"Nullable_Object", "unknown"},
            {"List_Int64", "string[]"},
            {"List_Int32", "number[]"},
            {"List_Decimal", "number[]"},
            {"List_Double", "number[]"},
            {"List_Float", "number[]"},
            {"List_Integer", "number[]"},
            {"List_Boolean", "boolean[]"},
            {"List_String", "string[]"},
            {"List_Object", "unknown[]"},
            {"SByte", "number"},
            {"Byte", "number"},
            {"Int16", "number"},
            {"Int64", "string"},
            {"Int32", "number"},
            {"UInt16", "number"},
            {"UInt32", "number"},
            {"UInt64", "number"},
            {"Single", "number"},
            {"Double", "number"},
            {"Decimal", "number"},
            {"Boolean", "boolean"},
            {"String", "string"},
            {"Char", "string"},
            {"Guid", "string"},
            {"Uri", "string"},
            {"DateTime", "string"},
            {"DateTimeOffset", "string"},
            {"DateOnly", "string"},
            {"TimeOnly", "string"},
            {"TimeSpan", "string"},
            {"Object", "unknown"},
            {"string", "string"},
            {"date-time", "string"},
            {"date", "string"},
            {"time", "string"},
            {"duration", "string"},
            {"uri", "string"},
            {"uri-reference", "string"},
            {"email", "string"},
            {"hostname", "string"},
            {"ipv4", "string"},
            {"ipv6", "string"},
            {"password", "string"},
            {"regex", "string"},
            {"int64", "string"},
            {"long", "string"},
            {"int32", "number"},
            {"int16", "number"},
            {"uint16", "number"},
            {"uint32", "number"},
            {"uint64", "number"},
            {"decimal", "number"},
            {"double", "number"},
            {"float", "number"},
            {"integer", "number"},
            {"number", "number"},
            {"boolean", "boolean"},
            {"uuid", "string"},
            {"byte", "string"},
            {"binary", "Blob"},
            {"object", "unknown"}
        };
    }
}

/// <summary>
/// OpenAPI 导入声明映射配置
/// </summary>
[SuppressSniffer]
public class OpenApiImportSchemaMappingSettingsOptions
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 映射名称
    /// </summary>
    /// <remarks>为空默认取 <see cref="Name"/></remarks>
    public string MappingName { get; set; }

    /// <summary>
    /// Web 端导入路径
    /// </summary>
    public string WebImportPath { get; set; }

    /// <summary>
    /// 移动端导入路径
    /// </summary>
    public string MobileImportPath { get; set; }
}

/// <summary>
/// OpenAPI 导入类型映射配置
/// </summary>
[SuppressSniffer]
public class OpenApiImportTypeMappingSettingsOptions
{
    /// <summary>
    /// 名称（开头）
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 映射名称
    /// </summary>
    /// <remarks>{0}取名称截取后的所有字符</remarks>
    public string MappingName { get; set; }

    /// <summary>
    /// 引用声明
    /// </summary>
    public HashSet<string> RefSchema { get; set; }
}
