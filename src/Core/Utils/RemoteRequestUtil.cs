// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections.Specialized;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Fast.NET.Core;

/// <summary>
/// <see cref="HttpClient"/> 远程请求工具类
/// </summary>
[SuppressSniffer]
public static class RemoteRequestUtil
{
    private static readonly Uri _daySentenceUri = new("https://open.iciba.com/dsapi/");

    /// <summary>
    /// 复用连接池，避免每次请求创建 HttpClient 导致端口耗尽
    /// </summary>
    private static readonly HttpClient _httpClient =
        new(new HttpClientHandler {AutomaticDecompression = DecompressionMethods.All}) {Timeout = Timeout.InfiniteTimeSpan};

    /// <summary>
    /// 默认 System.Text.Json 序列化配置
    /// </summary>
    private static readonly JsonSerializerOptions _defaultJsonSerializerOptions = new()
    {
        // 忽略对象图中的循环引用，避免远程响应序列化失败
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        // 显式指定 UTF-8 JSON 媒体类型，避免响应编码被错误推断
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 得到每日一句
    /// </summary>
    /// <returns>表示异步得到每日一句的任务，任务结果为得到每日一句</returns>
    public static async Task<DaySentenceInfo> GetDaySentence()
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(_daySentenceUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<DaySentenceInfo>(responseBody);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Day sentence request error，{ex.Message}", ex);
        }
    }

    /// <summary>
    /// 发送 HTTP GET 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="param">请求参数对象</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>HTTP Get 请求</returns>
    public static (T result, HttpResponseHeaders headers) Get<T>(string url, object param = null,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Get, url, param, null, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP GET 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="param">请求参数对象</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>表示异步 HTTP Get 请求的任务，任务结果为 HTTP Get 请求</returns>
    public static Task<(T result, HttpResponseHeaders headers)> GetAsync<T>(string url, object param = null,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Get, url, param, null, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP GET 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="param">请求参数对象</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>HTTP Get 请求</returns>
    public static (string result, HttpResponseHeaders headers) Get(string url, object param = null,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Get, url, param, null, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP GET 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="param">请求参数对象</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>表示异步 HTTP Get 请求的任务，任务结果为 HTTP Get 请求</returns>
    public static Task<(string result, HttpResponseHeaders headers)> GetAsync(string url, object param = null,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Get, url, param, null, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP POST 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>HTTP Post 请求</returns>
    public static (T result, HttpResponseHeaders headers) Post<T>(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Post, url, null, data, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP POST 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>表示异步 HTTP Post 请求的任务，任务结果为 HTTP Post 请求</returns>
    public static Task<(T result, HttpResponseHeaders headers)> PostAsync<T>(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Post, url, null, data, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP POST 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>HTTP Post 请求</returns>
    public static (string result, HttpResponseHeaders headers) Post(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Post, url, null, data, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP POST 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>表示异步 HTTP Post 请求的任务，任务结果为 HTTP Post 请求</returns>
    public static Task<(string result, HttpResponseHeaders headers)> PostAsync(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Post, url, null, data, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP PUT 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>HTTP Put 请求</returns>
    public static (T result, HttpResponseHeaders headers) Put<T>(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Put, url, null, data, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP PUT 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>表示异步 HTTP Put 请求的任务，任务结果为 HTTP Put 请求</returns>
    public static Task<(T result, HttpResponseHeaders headers)> PutAsync<T>(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Put, url, null, data, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP PUT 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>HTTP Put 请求</returns>
    public static (string result, HttpResponseHeaders headers) Put(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Put, url, null, data, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP PUT 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="data">要处理或传输的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>表示异步 HTTP Put 请求的任务，任务结果为 HTTP Put 请求</returns>
    public static Task<(string result, HttpResponseHeaders headers)> PutAsync(string url, object data,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Put, url, null, data, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP DELETE 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>HTTP Delete 请求</returns>
    public static (T result, HttpResponseHeaders headers) Delete<T>(string url, IDictionary<string, string> headers = null,
        bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Delete, url, null, null, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP DELETE 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>表示异步 HTTP Delete 请求的任务，任务结果为 HTTP Delete 请求</returns>
    public static Task<(T result, HttpResponseHeaders headers)> DeleteAsync<T>(string url,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60) where T : class
    {
        return SendAsync<T>(HttpMethod.Delete, url, null, null, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送 HTTP DELETE 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>HTTP Delete 请求</returns>
    public static (string result, HttpResponseHeaders headers) Delete(string url, IDictionary<string, string> headers = null,
        bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Delete, url, null, null, headers, paramEncode, timeout)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// 异步发送 HTTP DELETE 请求
    /// </summary>
    /// <param name="url">请求的 URL</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">请求参数使用的文本编码</param>
    /// <param name="timeout">超时时间，单位为毫秒</param>
    /// <returns>表示异步 HTTP Delete 请求的任务，任务结果为 HTTP Delete 请求</returns>
    public static Task<(string result, HttpResponseHeaders headers)> DeleteAsync(string url,
        IDictionary<string, string> headers = null, bool paramEncode = false, int? timeout = 60)
    {
        return SendAsync(HttpMethod.Delete, url, null, null, headers, paramEncode, timeout);
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="httpMethod">HTTP 请求方法</param>
    /// <param name="url">请求的 URL</param>
    /// <param name="urlParam">要追加到请求地址的查询参数</param>
    /// <param name="bodyData">要写入请求正文的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">是否对 URL 参数进行编码，默认 <see langword="false"/></param>
    /// <param name="timeout">请求超时时间，默认 60 秒，<see langword="null"/> 则不超时</param>
    /// <typeparam name="T">响应正文反序列化后的模型类型</typeparam>
    /// <returns>表示异步发送请求的任务，任务结果为发送请求</returns>
    public static async Task<(T result, HttpResponseHeaders headers)> SendAsync<T>(HttpMethod httpMethod, string url,
        object urlParam = null, object bodyData = null, IDictionary<string, string> headers = null, bool paramEncode = false,
        int? timeout = 60)
    {
        (string responseContent, HttpResponseHeaders responseHeaders) =
            await SendAsync(httpMethod, url, urlParam, bodyData, headers, paramEncode, timeout).ConfigureAwait(false);

        return (JsonSerializer.Deserialize<T>(responseContent, _defaultJsonSerializerOptions), responseHeaders);
    }

    /// <summary>
    /// 发送请求
    /// </summary>
    /// <param name="httpMethod">HTTP 请求方法</param>
    /// <param name="url">请求的 URL</param>
    /// <param name="urlParam">要追加到请求地址的查询参数</param>
    /// <param name="bodyData">要写入请求正文的数据</param>
    /// <param name="headers">随请求发送的 HTTP 标头集合</param>
    /// <param name="paramEncode">是否对 URL 参数进行编码，默认 <see langword="false"/></param>
    /// <param name="timeout">请求超时时间，默认 60 秒，<see langword="null"/> 则不超时</param>
    /// <returns>表示异步发送请求的任务，任务结果为发送请求</returns>
    public static async Task<(string result, HttpResponseHeaders headers)> SendAsync(HttpMethod httpMethod, string url,
        object urlParam = null, object bodyData = null, IDictionary<string, string> headers = null, bool paramEncode = false,
        int? timeout = 60)
    {
        ArgumentNullException.ThrowIfNull(httpMethod);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("请求 URL 不能为空。", nameof(url));
        }

        if (timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "超时时间必须大于 0 秒，或使用 null 表示不超时。");
        }

        headers ??= new Dictionary<string, string>();

        using CancellationTokenSource timeoutTokenSource =
            timeout.HasValue ? new CancellationTokenSource(TimeSpan.FromSeconds(timeout.Value)) : null;
        CancellationToken cancellationToken = timeoutTokenSource?.Token ?? CancellationToken.None;

        // 处理请求 URL
        var reqUriBuilder = new UriBuilder(url);

        // 处理 URL 本身自带的参数
        NameValueCollection query = HttpUtility.ParseQueryString(reqUriBuilder.Query);

        // 请求参数处理
        if (urlParam != null)
        {
            // 判断是否原本就为字典
            if (urlParam is IDictionary<string, object> paramObjDic)
            {
                foreach (KeyValuePair<string, object> param in paramObjDic)
                {
                    if (param.Value != null)
                    {
                        //query[param.Key.UrlEncode()] = param.Value.ToString()
                        if (paramEncode)
                        {
                            query[param.Key] = param.Value.ToString().UrlEncode();
                        }
                        else
                        {
                            query[param.Key] = param.Value.ToString();
                        }
                    }
                }
            }
            else if (urlParam is IDictionary<string, string> paramStrDic)
            {
                foreach (KeyValuePair<string, string> param in paramStrDic)
                {
                    if (!string.IsNullOrEmpty(param.Value))
                    {
                        //query[param.Key.UrlEncode()] = param.Value.UrlEncode()
                        if (paramEncode)
                        {
                            query[param.Key] = param.Value.UrlEncode();
                        }
                        else
                        {
                            query[param.Key] = param.Value;
                        }
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<string, object> param in urlParam.ToDictionary())
                {
                    if (param.Value != null)
                    {
                        //query[param.Key.UrlEncode()] = param.Value?.ToString()
                        if (paramEncode)
                        {
                            query[param.Key] = param.Value.ToString().UrlEncode();
                        }
                        else
                        {
                            query[param.Key] = param.Value.ToString();
                        }
                    }
                }
            }
        }

        // 设置 URL 参数
        reqUriBuilder.Query = query.ToString();

        using var request = new HttpRequestMessage();

        request.RequestUri = reqUriBuilder.Uri;
        request.Method = httpMethod;
        // 使用与常见浏览器一致的默认协商头，调用方提供的请求头随后覆盖或补充
        request.Headers.Add("Accept", "application/json, text/plain, */*");
        request.Headers.Add("Accept-Encoding", "gzip, compress, deflate, br");
        request.Headers.Referrer = reqUriBuilder.Uri;

        request.Headers.TryAddWithoutValidation("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.81 Safari/537.36 Edg/104.0.1293.47");

        foreach (KeyValuePair<string, string> header in headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Body 参数处理
        if (bodyData != null)
        {
            // 判断是否原本就为字符串
            if (bodyData is string dataStr)
            {
                var httpContent = new StringContent(dataStr, Encoding.UTF8, "application/json");

                request.Content = httpContent;
            }
            else
            {
                // 请求数据转为 JSON 字符串
                string reqBodyDataJson = JsonSerializer.Serialize(bodyData, _defaultJsonSerializerOptions);

                var httpContent = new StringContent(reqBodyDataJson, Encoding.UTF8, "application/json");

                request.Content = httpContent;
            }
        }

        string responseContent = null;

        try
        {
            using HttpResponseMessage response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);

            byte[] responseContentBytes;

            // 判断是否启用了响应压缩
            if (response.Content.Headers.ContentEncoding.Contains("br"))
            {
                // Brotli 解压缩
                byte[] responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                using var compressedStream = new MemoryStream(responseBytes);
                using var decompressedStream = new MemoryStream();
                await using var brotliStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
                await brotliStream.CopyToAsync(decompressedStream, cancellationToken).ConfigureAwait(false);
                responseContentBytes = decompressedStream.ToArray();
            }
            else if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                // Gzip 解压缩
                byte[] responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                using var compressedStream = new MemoryStream(responseBytes);
                using var decompressedStream = new MemoryStream();
                await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                await gzipStream.CopyToAsync(decompressedStream, cancellationToken).ConfigureAwait(false);
                responseContentBytes = decompressedStream.ToArray();
            }
            else if (response.Content.Headers.ContentEncoding.Contains("deflate"))
            {
                // Deflate  解压缩
                byte[] responseBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                using var compressedStream = new MemoryStream(responseBytes);
                using var decompressedStream = new MemoryStream();
                await using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                await deflateStream.CopyToAsync(decompressedStream, cancellationToken).ConfigureAwait(false);
                responseContentBytes = decompressedStream.ToArray();
            }
            else
            {
                responseContentBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            // 获取 charset 编码
            Encoding encoding = GetCharsetEncoding(response);
            // 通过指定编码解码
            responseContent = encoding.GetString(responseContentBytes);

            // 先解码响应正文，再检查状态码；失败时可将服务端的结构化错误转换为更有意义的异常
            response.EnsureSuccessStatusCode();

            return (responseContent, response.Headers);
        }
        catch (HttpRequestException ex)
        {
            if (string.IsNullOrEmpty(responseContent))
            {
                throw;
            }

            // 仅识别带 Message 和 StackTrace 的框架异常响应，其他错误正文保持原样返回
            object responseData = null;
            try
            {
                responseData = JsonSerializer.Deserialize<IDictionary<string, object>>(responseContent,
                    _defaultJsonSerializerOptions);
            }
            catch
            {
                // 非 JSON 错误正文将在下方直接作为异常消息返回
            }

            if (responseData is IDictionary<string, object> responseDataDictionary)
            {
                if (responseDataDictionary.TryGetValue("Message", out object responseMessage)
                    && responseDataDictionary.TryGetValue("StackTrace", out _))
                {
                    throw new HttpRequestException(responseMessage.ToString(), ex);
                }
            }

            throw new HttpRequestException(responseContent, ex);
        }
        catch (OperationCanceledException ex) when (timeoutTokenSource?.IsCancellationRequested == true)
        {
            throw new TimeoutException("远程请求超时：" + ex.Message, ex);
        }
    }

    /// <summary>
    /// 将一个字符串 URL 编码 <para>如果已经 URL 编码则不会继续编码</para>
    /// </summary>
    /// <param name="str">要编码的字符串</param>
    /// <returns>URL 编码后的字符串；输入为空或已编码时返回对应的原值</returns>
    private static string UrlEncode(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return "";
        }

        string result = HttpUtility.UrlEncode(str, Encoding.UTF8);

        try
        {
            // 解码结果变化说明输入已经包含转义序列，此时保留原字符串以避免二次编码
            string tryDecode = HttpUtility.UrlDecode(str, Encoding.UTF8);
            return str.Equals(tryDecode, StringComparison.OrdinalIgnoreCase) ? result : str;
        }
        catch
        {
            // 无法可靠判断原字符串是否已编码时，返回本次编码结果
            return result;
        }
    }

    /// <summary>
    /// 将一个 Object 对象转为 字典
    /// </summary>
    /// <param name="obj">要读取公共属性的对象</param>
    /// <param name="includeNull">是否包含值为 <see langword="null"/> 的属性</param>
    /// <returns>以属性名为键、属性值为值的字典</returns>
    private static IDictionary<string, object> ToDictionary(this object obj, bool includeNull = false)
    {
        var dictionary = new Dictionary<string, object>();

        Type t = obj.GetType(); // 获取对象对应的类， 对应的类型

        PropertyInfo[] pi = t.GetProperties(BindingFlags.Public | BindingFlags.Instance); // 获取当前type公共属性

        foreach (PropertyInfo p in pi)
        {
            MethodInfo m = p.GetGetMethod();

            if (m == null || !m.IsPublic)
            {
                continue;
            }

            object value = m.Invoke(obj, Array.Empty<object>());
            if (value != null || includeNull)
            {
                dictionary.Add(p.Name, value); // 向字典添加元素
            }
        }

        return dictionary;
    }

    /// <summary>
    /// 获取响应报文 charset 编码
    /// </summary>
    /// <param name="response">响应消息</param>
    /// <returns>获取到的响应报文 charset 编码</returns>
    private static Encoding GetCharsetEncoding(HttpResponseMessage response)
    {
        if (response == null)
        {
            return Encoding.UTF8;
        }

        // 获取 charset
        string charset;

        bool withContentType = response.Content.Headers.TryGetValues("Content-Type", out IEnumerable<string> contentTypes);
        if (withContentType)
        {
            charset = contentTypes.First()
                          .Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .FirstOrDefault(u => u.Contains("charset", StringComparison.OrdinalIgnoreCase))
                      ?? "charset=UTF-8";
        }
        else
        {
            charset = "charset=UTF-8";
        }

        string encoding = charset.Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "UTF-8";
        return Encoding.GetEncoding(encoding);
    }
}
