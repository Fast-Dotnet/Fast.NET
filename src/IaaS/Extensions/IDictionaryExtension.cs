// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Fast.IaaS;

/// <summary>
/// 为 <see cref="IDictionary{TKey,TValue}"/> 提供扩展方法
/// </summary>
public static class IDictionaryExtension
{
    /// <summary>
    /// 将一个字典转化为 QueryString
    /// </summary>
    /// <param name="dict">要处理的字典</param>
    /// <param name="urlEncode">是否对生成的查询字符串执行 URL 编码</param>
    /// <param name="isToLower">是否将名称的首字母转换为小写</param>
    /// <returns>将一个字典转化为 QueryString</returns>
    public static string ToQueryString(this IDictionary<string, string> dict, bool urlEncode = true, bool isToLower = false)
    {
        return string.Join("&",
            dict.Select(p =>
                $"{(urlEncode ? isToLower ? p.Key?.FirstCharToLower().UrlEncode() : p.Key?.UrlEncode() : "")}={(urlEncode ? p.Value?.UrlEncode() : "")}"));
    }

    /// <summary>
    /// 移除空值项
    /// </summary>
    /// <param name="dict">IDictionary{TKey,TValue}</param>
    public static void RemoveEmptyValueItems(this IDictionary<string, string> dict)
    {
        dict.Where(item => string.IsNullOrEmpty(item.Value))
            .Select(item => item.Key)
            .ToList()
            .ForEach(key => { dict.Remove(key); });
    }

    /// <summary>
    /// 添加或更新
    /// </summary>
    /// <param name="dictionary">IDictionary{TKey, TValue}</param>
    /// <param name="key">字典键</param>
    /// <param name="value">要添加或替换的字典值</param>
    /// <typeparam name="TKey">字典键类型</typeparam>
    /// <typeparam name="TValue">字典值类型</typeparam>
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        where TKey : notnull
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (!dictionary.TryGetValue(key, out List<TValue> values))
        {
            values = new List<TValue>();
            dictionary.Add(key, values);
        }

        values.Add(value);
    }

    /// <summary>
    /// 添加或更新
    /// </summary>
    /// <param name="dictionary">IDictionary{TKey, TValue}</param>
    /// <param name="concatDictionary">要与当前字典合并的数据</param>
    /// <typeparam name="TKey">字典键类型</typeparam>
    /// <typeparam name="TValue">字典值类型</typeparam>
    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary,
        IDictionary<TKey, List<TValue>> concatDictionary) where TKey : notnull
    {
        if (concatDictionary is null)
        {
            throw new ArgumentNullException(nameof(concatDictionary));
        }

        foreach ((TKey key, List<TValue> newValues) in concatDictionary)
        {
            if (!dictionary.TryGetValue(key, out List<TValue> values))
            {
                values = new List<TValue>();
                dictionary.Add(key, values);
            }

            values.AddRange(newValues);
        }
    }

    /// <summary>
    /// 添加或更新
    /// </summary>
    /// <param name="dictionary">IDictionary{TKey, TValue}</param>
    /// <param name="concatDictionary">要与当前字典合并的数据</param>
    /// <typeparam name="TKey">字典键类型</typeparam>
    /// <typeparam name="TValue">字典值类型</typeparam>
    public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        IDictionary<TKey, TValue> concatDictionary) where TKey : notnull
    {
        if (concatDictionary is null)
        {
            throw new ArgumentNullException(nameof(concatDictionary));
        }

        foreach ((TKey key, TValue value) in concatDictionary)
        {
            dictionary[key] = value;
        }
    }

    /// <summary>
    /// 合并两个字典
    /// </summary>
    /// <param name="dic">IDictionary{TKey,TValue}字典</param>
    /// <param name="newDic">IDictionary{TKey,TValue}新字典</param>
    /// <typeparam name="T">字典值的类型</typeparam>
    /// <returns>IDictionary{TKey,TValue}</returns>
    public static IDictionary<string, T> AddOrUpdate<T>(this IDictionary<string, T> dic, IDictionary<string, T> newDic)
    {
        foreach (string key in newDic.Keys)
        {
            if (dic.TryGetValue(key, out T value))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, newDic[key]);
            }
        }

        return dic;
    }


    /// <summary>
    /// 将 Dic 字典转换成字符串
    /// </summary>
    /// <param name="dic">IDictionary{TKey,TValue}</param>
    /// <returns>将 Dic 字典转换成字符串</returns>
    public static string DicToXmlStr(this IDictionary<string, object> dic)
    {
        string xml = "<xml>";
        foreach ((string key, object value) in dic)
        {
            if (value is int)
            {
                xml += "<" + key + ">" + value + "</" + key + ">";
            }
            else if (value is string)
            {
                xml += "<" + key + ">" + "<![CDATA[" + value + "]]></" + key + ">";
            }
        }

        xml += "</xml>";
        return xml;
    }

    /// <summary>
    /// 将字符串转换为 Dic 字典
    /// </summary>
    /// <param name="xml">要解析的 XML 文本</param>
    /// <returns>IDictionary{TKey,TValue}</returns>
    public static IDictionary<string, object> XmlStrToDic(this string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("不能转换空字符串！", nameof(xml));
        }

        var rltDic = new Dictionary<string, object>();
        var xmlDoc = new XmlDocument {XmlResolver = null};
        xmlDoc.LoadXml(xml);
        XmlNode xmlNode = xmlDoc.FirstChild; //获取到根节点<xml>
        if (xmlNode != null)
        {
            XmlNodeList nodes = xmlNode.ChildNodes;
            foreach (XmlNode xn in nodes)
            {
                var xe = (XmlElement)xn;
                rltDic[xe.Name] = xe.InnerText; //获取xml的键值对到WxPayData内部的数据中
            }
        }

        return rltDic;
    }

    /// <summary>
    /// 将 Dic 字典转换成字符串
    /// </summary>
    /// <param name="dic">IDictionary{TKey,TValue}</param>
    /// <returns>将 Dic 字典转换成字符串</returns>
    public static string SortDicToXmlStr(this SortedDictionary<string, object> dic)
    {
        string xml = "<xml>";
        foreach ((string key, object value) in dic)
        {
            if (value is int)
            {
                xml += "<" + key + ">" + value + "</" + key + ">";
            }
            else if (value is string)
            {
                xml += "<" + key + ">" + "<![CDATA[" + value + "]]></" + key + ">";
            }
        }

        xml += "</xml>";
        return xml;
    }

    /// <summary>
    /// 将字符串转换为 Dic 字典
    /// </summary>
    /// <param name="xml">要解析的 XML 文本</param>
    /// <returns>SortedDictionary{TKey,TValue}</returns>
    public static SortedDictionary<string, object> XmlStrToSortDic(this string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("不能转换空字符串！", nameof(xml));
        }

        var rltDic = new SortedDictionary<string, object>();
        var xmlDoc = new XmlDocument {XmlResolver = null};
        xmlDoc.LoadXml(xml);
        XmlNode xmlNode = xmlDoc.FirstChild; //获取到根节点<xml>
        if (xmlNode != null)
        {
            XmlNodeList nodes = xmlNode.ChildNodes;
            foreach (XmlNode xn in nodes)
            {
                var xe = (XmlElement)xn;
                rltDic[xe.Name] = xe.InnerText; //获取xml的键值对到WxPayData内部的数据中
            }
        }

        return rltDic;
    }
}
