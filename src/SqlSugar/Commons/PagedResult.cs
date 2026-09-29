// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

namespace Fast.SqlSugar;

/// <summary>
/// SqlSugar 统一分页返回结果类
/// </summary>
/// <typeparam name="TResult">操作结果类型</typeparam>
[SuppressSniffer]
public class PagedResult<TResult>
{
    /// <summary>
    /// 当前页
    /// </summary>
    public virtual int PageIndex { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public virtual int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public virtual int TotalPage { get; set; }

    /// <summary>
    /// 总条数
    /// </summary>
    public virtual int TotalRows { get; set; }

    /// <summary>
    /// Data
    /// </summary>
    public virtual IEnumerable<TResult> Rows { get; set; }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public virtual bool HasPrevPages { get; set; }

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public virtual bool HasNextPages { get; set; }

    private string _assemblyName { get; set; }

    /// <summary>
    /// 程序集名称
    /// </summary>
    public virtual string AssemblyName
    {
        get
        {
            if (string.IsNullOrEmpty(_assemblyName))
            {
                _assemblyName = typeof(TResult).Assembly.GetName().Name;
            }

            return _assemblyName;
        }
        set => _assemblyName = value;
    }

    private string _fullName { get; set; }

    /// <summary>
    /// 类型完全限定名称
    /// </summary>
    public virtual string TypeFullName
    {
        get
        {
            if (string.IsNullOrEmpty(_fullName))
            {
                _fullName = typeof(TResult).FullName;
            }

            return _fullName;
        }
        set => _fullName = value;
    }
}
