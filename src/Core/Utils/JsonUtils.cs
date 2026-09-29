// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Globalization;
using System.Text.Json;

namespace Fast.NET.Core;

/// <summary>
/// JSON 工具类
/// </summary>
[SuppressSniffer]
public static class JsonUtils
{
    /// <summary>
    /// 读取 JSON 文件
    /// </summary>
    /// <param name="path">目标路径</param>
    /// <returns>读取到的 JSON 文件</returns>
    public static IDictionary<string, string> ReadJsonFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(path);
        }

        // 读取文件内容
        using var streamReader = new StreamReader(path);
        string json = streamReader.ReadToEnd();

        // 将 JSON 文本解析为节点树
        return ParseJson(json);
    }

    /// <summary>
    /// 解析 JSON 字符串
    /// </summary>
    /// <param name="json">要解析的 JSON 文本</param>
    /// <returns>解析后的 JSON 字符串</returns>
    public static IDictionary<string, string> ParseJson(string json)
    {
        var options = new JsonDocumentOptions {AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip};

        var dictionary = new Dictionary<string, string>();

        using var doc = JsonDocument.Parse(json, options);
        JsonElement root = doc.RootElement;

        VisitJsonElement(root, dictionary);

        return dictionary;
    }

    /// <summary>
    /// 递归访问 JSON 节点并将叶子值写入扁平化字典
    /// </summary>
    /// <param name="root">当前 JSON 节点</param>
    /// <param name="dictionary">用于保存扁平化结果的字典</param>
    /// <param name="currentPath">当前节点的路径</param>
    private static void VisitJsonElement(JsonElement root, IDictionary<string, string> dictionary, string currentPath = "")
    {
        // 判断是否为对象
        switch (root.ValueKind)
        {
            case JsonValueKind.Undefined:
                break;
            case JsonValueKind.Null:
                {
                    dictionary.TryAdd(currentPath, null);
                }
                break;
            // 对象
            case JsonValueKind.Object:
                {
                    foreach (JsonProperty property in root.EnumerateObject())
                    {
                        string newPath = string.IsNullOrWhiteSpace(currentPath)
                            ? property.Name
                            : $"{currentPath}:{property.Name}";
                        VisitJsonElement(property.Value, dictionary, newPath);
                    }
                }
                break;
            // 数组
            case JsonValueKind.Array:
                {
                    for (int index = 0; index < root.GetArrayLength(); index++)
                    {
                        string newPath = string.IsNullOrWhiteSpace(currentPath) ? $"{index}" : $"{currentPath}:{index}";
                        VisitJsonElement(root[index], dictionary, newPath);
                    }
                }
                break;
            // 字符串
            case JsonValueKind.String:
                {
                    // 去除转义字符
                    string unescapedValue = root.GetString()?.Replace("\\", "");
                    dictionary.TryAdd(currentPath, unescapedValue);
                }
                break;
            case JsonValueKind.Number:
                {
                    if (root.TryGetInt32(out int intVal))
                    {
                        dictionary.TryAdd(currentPath, intVal.ToString());
                    }
                    else if (root.TryGetDouble(out double doubleVal))
                    {
                        dictionary.TryAdd(currentPath, doubleVal.ToString(CultureInfo.InvariantCulture));
                    }
                    else if (root.TryGetInt64(out long longVal))
                    {
                        dictionary.TryAdd(currentPath, longVal.ToString());
                    }
                    else
                    {
                        dictionary.TryAdd(currentPath, root.GetRawText());
                    }
                }
                break;
            case JsonValueKind.True:
                {
                    dictionary.TryAdd(currentPath, true.ToString());
                }
                break;
            case JsonValueKind.False:
                {
                    dictionary.TryAdd(currentPath, false.ToString());
                }
                break;
        }
    }
}
