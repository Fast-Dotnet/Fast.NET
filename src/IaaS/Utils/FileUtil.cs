// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.IO;
using System.Security.Cryptography;

namespace Fast.IaaS;

/// <summary>
/// 文件工具类
/// </summary>
public static class FileUtil
{
    /// <summary>
    /// 获取文件的 SHA-256 哈希值
    /// </summary>
    /// <param name="filePath">file Path 路径</param>
    /// <returns>获取到的文件的 SHA-256 哈希值</returns>
    public static string GetFileSHA256(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空。", nameof(filePath));
        }

        using var sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);
        return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// 获取文件的 SHA1 哈希值
    /// </summary>
    /// <param name="filePath">file Path 路径</param>
    /// <returns>由小写字母组成的 SHA1 哈希值字符串</returns>
    public static string GetFileSHA1(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("文件路径不能为空。", nameof(filePath));
        }

        // 创建 SHA1 实例
        using var osha1 = SHA1.Create();

        // 打开文件流，读取文件内容（使用 using 确保异常时也能释放文件句柄）
        using var oFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // 计算文件的 SHA1 哈希值
        byte[] arrBytHashValue = osha1.ComputeHash(oFileStream);

        // 将哈希值转换为十六进制字符串，并去掉连字符（"-"），转换为小写
        return BitConverter.ToString(arrBytHashValue).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// 复制文件
    /// </summary>
    /// <param name="fromPath">from Path 路径</param>
    /// <param name="toPath">to Path 路径</param>
    public static void CopyFile(string fromPath, string toPath)
    {
        if (!File.Exists(fromPath))
        {
            throw new FileNotFoundException("源文件不存在！");
        }

        string destinationDirectory = Path.GetDirectoryName(toPath);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // 复制文件
        File.Copy(fromPath, toPath, true);
    }

    /// <summary>
    /// 尝试创建文件夹
    /// </summary>
    /// <param name="path">路径</param>
    public static void TryCreateDirectory(string path)
    {
        string destinationDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }
    }
}
