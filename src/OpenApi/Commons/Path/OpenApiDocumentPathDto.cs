// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.OpenApi;

/// <summary>
/// OpenAPI 文档路由 DTO
/// </summary>
public class OpenApiDocumentPathDto
{
    /// <summary>
    /// Get 请求
    /// </summary>
    public OpenApiDocumentPathMethodDto Get { get; set; }

    /// <summary>
    /// Post 请求
    /// </summary>
    public OpenApiDocumentPathMethodDto Post { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public OpenApiDocumentPathMethodDto Method
    {
        get
        {
            if (Get != null)
            {
                return Get;
            }

            if (Post != null)
            {
                return Post;
            }

            return Get;
        }
    }

    /// <summary>
    /// 请求方式
    /// </summary>
    public HttpRequestMethodEnum RequestMethod
    {
        get
        {
            if (Get != null)
            {
                return HttpRequestMethodEnum.Get;
            }

            if (Post != null)
            {
                return HttpRequestMethodEnum.Post;
            }

            return HttpRequestMethodEnum.Get;
        }
    }

    /// <summary>
    /// 模块
    /// </summary>
    public string Tag
    {
        get
        {
            switch (RequestMethod)
            {
                case HttpRequestMethodEnum.Get:
                    return Get.Tags?.FirstOrDefault();
                case HttpRequestMethodEnum.Post:
                    return Post.Tags?.FirstOrDefault();
                case HttpRequestMethodEnum.Put:
                case HttpRequestMethodEnum.Delete:
                case HttpRequestMethodEnum.Patch:
                case HttpRequestMethodEnum.Head:
                case HttpRequestMethodEnum.Options:
                case HttpRequestMethodEnum.Connect:
                case HttpRequestMethodEnum.Trace:
                default:
                    return null;
            }
        }
    }
}
