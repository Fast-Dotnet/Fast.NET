// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text;
using System.Text.Json;

namespace Fast.Consul;

/// <summary>
/// Consul Key/Value 响应 DTO
/// </summary>
internal sealed class ConsulKeyValueResponseDto
{
    public int LockIndex { get; set; }

    public string Key { get; set; }

    public int Flags { get; set; }

    public string Value { get; set; }

    public long CreateIndex { get; set; }

    public long ModifyIndex { get; set; }
}

/// <inheritdoc cref="IKeyValueService" />
public class KeyValueService : IKeyValueService
{
    /// <summary>
    /// Key/Value 请求共用的 HTTP 客户端
    /// </summary>
    private static readonly HttpClient _httpClient = new() {Timeout = TimeSpan.FromSeconds(60)};

    /// <summary>
    /// Consul Key/Value 响应的 JSON 反序列化配置
    /// </summary>
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {PropertyNameCaseInsensitive = true};

    /// <summary>
    /// 向 Consul 发送 GET 请求并反序列化响应正文
    /// </summary>
    /// <param name="requestUri">Consul Key/Value 请求地址</param>
    /// <typeparam name="T">响应正文反序列化后的类型</typeparam>
    /// <returns>反序列化后的响应内容</returns>
    private static async Task<T> Get<T>(string requestUri)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUri).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // 优先读取响应正文，使异常能够保留 Consul 返回的具体错误信息
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseContent, null, response.StatusCode);
        }

        return JsonSerializer.Deserialize<T>(responseContent, _jsonSerializerOptions);
    }

    /// <summary>
    /// 向 Consul 发送 PUT 请求并返回响应正文
    /// </summary>
    /// <param name="requestUri">Consul Key/Value 请求地址</param>
    /// <param name="data">要写入 Consul 的原始 UTF-8 文本</param>
    /// <returns>Consul 返回的响应正文</returns>
    private static async Task<string> Put(string requestUri, string data)
    {
        using StringContent content = data == null ? null : new StringContent(data, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _httpClient.PutAsync(requestUri, content).ConfigureAwait(false);
        string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // 与 GET 保持一致，失败响应直接携带 Consul 返回的正文
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(responseContent, null, response.StatusCode);
        }

        return responseContent;
    }

    /// <inheritdoc />
    public async Task<T> GetKeyValue<T>(string settingPath, string dcName)
    {
        ValidatePath(settingPath, dcName);
        List<ConsulKeyValueResponseDto>
            result = await Get<List<ConsulKeyValueResponseDto>>(BuildKeyValueUrl(settingPath, dcName));

        if (result == null || result.Count == 0)
        {
            throw new KeyNotFoundException("未找到指定 Consul 配置！");
        }

        string value = result[0].Value;

        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(Convert.FromBase64String(value)));
    }

    /// <inheritdoc />
    public async Task<string> GetKeyValue(string settingPath, string dcName)
    {
        ValidatePath(settingPath, dcName);
        List<ConsulKeyValueResponseDto>
            result = await Get<List<ConsulKeyValueResponseDto>>(BuildKeyValueUrl(settingPath, dcName));

        if (result == null || result.Count == 0)
        {
            throw new KeyNotFoundException("未找到指定 Consul 配置！");
        }

        string value = result[0].Value;

        return Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    /// <inheritdoc />
    public async Task<bool> EditKeyValue(string settingPath, string dcName, string data)
    {
        ValidatePath(settingPath, dcName);
        string responseContent = await Put($"{BuildKeyValueUrl(settingPath, dcName)}&flags=0", data);

        return bool.TryParse(responseContent, out bool result) && result;
    }

    private static string BuildKeyValueUrl(string settingPath, string dcName)
    {
        return $"{Penetrates.ConsulSettings.Address.TrimEnd('/')}/v1/kv/{Uri.EscapeDataString(settingPath)}"
               + $"?dc={Uri.EscapeDataString(dcName)}";
    }

    private static void ValidatePath(string settingPath, string dcName)
    {
        if (string.IsNullOrWhiteSpace(settingPath))
        {
            throw new ArgumentException("Consul 配置路径不能为空。", nameof(settingPath));
        }

        if (string.IsNullOrWhiteSpace(dcName))
        {
            throw new ArgumentException("Consul 数据中心名称不能为空。", nameof(dcName));
        }
    }
}
