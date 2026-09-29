// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;

namespace Fast.IaaS;

/// <summary>
/// 加密解密工具类
/// </summary>
public static class CryptoUtil
{
    /// <summary>
    /// PBKDF2 默认迭代次数
    /// </summary>
    private const int DEFAULT_PBKDF2_ITERATIONS = 600000;

    /// <summary>
    /// PBKDF2 允许的最小迭代次数
    /// </summary>
    private const int MINIMUM_PBKDF2_ITERATIONS = 100000;

    /// <summary>
    /// PBKDF2 允许的最大迭代次数，用于限制异常输入造成的计算资源消耗
    /// </summary>
    private const int MAXIMUM_PBKDF2_ITERATIONS = 5000000;

    /// <summary>
    /// 密码允许的最大 UTF-8 字节数
    /// </summary>
    private const int MAXIMUM_PASSWORD_BYTES = 1024;

    /// <summary>
    /// 密码加密接口允许的最大明文字节数
    /// </summary>
    private const int MAXIMUM_PLAINTEXT_BYTES = 8 * 1024 * 1024;

    /// <summary>
    /// 密码解密接口允许的最大协议载荷字符数
    /// </summary>
    private const int MAXIMUM_PAYLOAD_LENGTH = 16 * 1024 * 1024;

    /// <summary>
    /// 密码加密载荷的协议与算法版本前缀
    /// </summary>
    private const string PASSWORD_ENCRYPTION_PREFIX = "FAST-AES-256-GCM-V1";

    /// <summary>
    /// PBKDF2 密码哈希的协议与算法版本前缀
    /// </summary>
    private const string PASSWORD_HASH_PREFIX = "FAST-PBKDF2-SHA256-V1";

    /// <summary>
    /// RSA rsaEncryption 的 DER OID 内容，不包含 0x06 标签和长度
    /// </summary>
    private static readonly byte[] RSA_ENCRYPTION_OID = {0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01};

    /// <summary>
    /// EC id-ecPublicKey 的 DER OID 内容，不包含 0x06 标签和长度
    /// </summary>
    private static readonly byte[] EC_PUBLIC_KEY_OID = {0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x02, 0x01};

    /// <summary>
    /// NIST P-256 曲线的 DER OID 内容，不包含 0x06 标签和长度
    /// </summary>
    private static readonly byte[] P256_OID = {0x2A, 0x86, 0x48, 0xCE, 0x3D, 0x03, 0x01, 0x07};

    /// <summary>
    /// NIST P-384 曲线的 DER OID 内容，不包含 0x06 标签和长度
    /// </summary>
    private static readonly byte[] P384_OID = {0x2B, 0x81, 0x04, 0x00, 0x22};

    /// <summary>
    /// NIST P-521 曲线的 DER OID 内容，不包含 0x06 标签和长度
    /// </summary>
    private static readonly byte[] P521_OID = {0x2B, 0x81, 0x04, 0x00, 0x23};

    /// <summary>
    /// 使用 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥表示的 PEM 密钥对
    /// </summary>
    public sealed class PemKeyPair
    {
        /// <summary>
        /// 初始化一组已编码的 PEM 私钥和公钥
        /// </summary>
        /// <param name="privateKey">未加密的 PKCS#8 PEM 私钥</param>
        /// <param name="publicKey">SubjectPublicKeyInfo PEM 公钥</param>
        internal PemKeyPair(string privateKey, string publicKey)
        {
            PrivateKey = privateKey;
            PublicKey = publicKey;
        }

        /// <summary>
        /// 未加密的 PKCS#8 PEM 私钥
        /// </summary>
        public string PrivateKey { get; }

        /// <summary>
        /// SubjectPublicKeyInfo PEM 公钥
        /// </summary>
        public string PublicKey { get; }
    }

    #region PEM/DER Helpers

    /// <summary>
    /// 把 DER 内容封装为每行 64 个 Base64 字符的 PEM
    /// </summary>
    /// <param name="label">PEM Begin/End 标签</param>
    /// <param name="value">DER 编码内容</param>
    /// <returns>带标准头尾的 PEM 文本</returns>
    private static string ToPem(string label, byte[] value)
    {
        string encoded = Convert.ToBase64String(value);
        var builder = new StringBuilder(encoded.Length + 64);
        builder.Append("-----BEGIN ").Append(label).AppendLine("-----");
        for (int index = 0; index < encoded.Length; index += 64)
        {
            builder.AppendLine(encoded.Substring(index, Math.Min(64, encoded.Length - index)));
        }

        builder.Append("-----END ").Append(label).Append("-----");
        return builder.ToString();
    }

    /// <summary>
    /// 验证 PEM 标签并提取 DER 内容
    /// </summary>
    /// <param name="value">待解析的 PEM 文本</param>
    /// <param name="label">当前算法要求的 PEM 标签</param>
    /// <returns>DER 编码内容</returns>
    /// <exception cref="CryptographicException">标签或 Base64 内容无效</exception>
    private static byte[] FromPem(string value, string label)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        string header = $"-----BEGIN {label}-----";
        string footer = $"-----END {label}-----";
        string trimmed = value.Trim();
        if (!trimmed.StartsWith(header, StringComparison.Ordinal) || !trimmed.EndsWith(footer, StringComparison.Ordinal))
        {
            throw new CryptographicException($"需要 {label} PEM 格式的密钥。");
        }

        string content = trimmed.Substring(header.Length, trimmed.Length - header.Length - footer.Length);
        var encoded = new StringBuilder(content.Length);
        foreach (char character in content)
        {
            if (!char.IsWhiteSpace(character))
            {
                encoded.Append(character);
            }
        }

        try
        {
            return Convert.FromBase64String(encoded.ToString());
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("PEM 密钥包含无效的 Base64 内容。", exception);
        }
    }

    /// <summary>
    /// 把完整 RSA 参数编码为 PKCS#8 PrivateKeyInfo DER
    /// </summary>
    /// <param name="parameters">包含私钥 CRT 参数的 RSA 参数</param>
    /// <returns>PKCS#8 DER 字节</returns>
    private static byte[] EncodeRsaPrivateKey(RSAParameters parameters)
    {
        // PrivateKeyInfo = version || rsaEncryption AlgorithmIdentifier || OCTET STRING(RSAPrivateKey)
        byte[] algorithm = EncodeDerSequence(EncodeDer(0x06, RSA_ENCRYPTION_OID), EncodeDer(0x05, Array.Empty<byte>()));
        // RSAPrivateKey 使用 PKCS#1 规定的 CRT 参数顺序，version=0 表示双素数 RSA
        byte[] privateKey = EncodeDerSequence(EncodeDerInteger(0), EncodeDerInteger(parameters.Modulus),
            EncodeDerInteger(parameters.Exponent), EncodeDerInteger(parameters.D), EncodeDerInteger(parameters.P),
            EncodeDerInteger(parameters.Q), EncodeDerInteger(parameters.DP), EncodeDerInteger(parameters.DQ),
            EncodeDerInteger(parameters.InverseQ));
        return EncodeDerSequence(EncodeDerInteger(0), algorithm, EncodeDer(0x04, privateKey));
    }

    /// <summary>
    /// 把 RSA 公钥参数编码为 SubjectPublicKeyInfo DER
    /// </summary>
    /// <param name="parameters">包含模数和指数的 RSA 参数</param>
    /// <returns>SubjectPublicKeyInfo DER 字节</returns>
    private static byte[] EncodeRsaPublicKey(RSAParameters parameters)
    {
        byte[] algorithm = EncodeDerSequence(EncodeDer(0x06, RSA_ENCRYPTION_OID), EncodeDer(0x05, Array.Empty<byte>()));
        // SubjectPublicKeyInfo 的 BIT STRING 内嵌 PKCS#1 RSAPublicKey(modulus, publicExponent)
        byte[] publicKey = EncodeDerSequence(EncodeDerInteger(parameters.Modulus), EncodeDerInteger(parameters.Exponent));
        return EncodeDerSequence(algorithm, EncodeDerBitString(publicKey));
    }

    /// <summary>
    /// 解析并验证 RSA PKCS#8 PrivateKeyInfo DER
    /// </summary>
    /// <param name="value">PKCS#8 DER 字节</param>
    /// <returns>可导入 <see cref="RSA"/> 的完整私钥参数</returns>
    private static RSAParameters ReadRsaPrivateKey(byte[] value)
    {
        DerReader outer = ReadSingleDerSequence(value);
        // 严格消费 PrivateKeyInfo 版本和算法标识，拒绝把其他算法的 PKCS#8 当作 RSA 导入
        outer.ReadInteger();
        ReadAlgorithmIdentifier(outer, RSA_ENCRYPTION_OID, null);
        DerReader privateKey = ReadSingleDerSequence(outer.ReadValue(0x04));
        privateKey.ReadInteger();
        var parameters = new RSAParameters
        {
            Modulus = privateKey.ReadInteger(),
            Exponent = privateKey.ReadInteger(),
            D = privateKey.ReadInteger(),
            P = privateKey.ReadInteger(),
            Q = privateKey.ReadInteger(),
            DP = privateKey.ReadInteger(),
            DQ = privateKey.ReadInteger(),
            InverseQ = privateKey.ReadInteger()
        };
        privateKey.EnsureEmpty();
        outer.EnsureEmpty();
        return parameters;
    }

    /// <summary>
    /// 解析并验证 RSA SubjectPublicKeyInfo DER
    /// </summary>
    /// <param name="value">SubjectPublicKeyInfo DER 字节</param>
    /// <returns>可导入 <see cref="RSA"/> 的公钥参数</returns>
    private static RSAParameters ReadRsaPublicKey(byte[] value)
    {
        DerReader outer = ReadSingleDerSequence(value);
        ReadAlgorithmIdentifier(outer, RSA_ENCRYPTION_OID, null);
        DerReader publicKey = ReadSingleDerSequence(outer.ReadBitString());
        var parameters = new RSAParameters {Modulus = publicKey.ReadInteger(), Exponent = publicKey.ReadInteger()};
        publicKey.EnsureEmpty();
        outer.EnsureEmpty();
        return parameters;
    }

    /// <summary>
    /// 把 EC 私钥参数编码为 PKCS#8 PrivateKeyInfo DER
    /// </summary>
    /// <param name="parameters">包含私钥标量和公钥点的 EC 参数</param>
    /// <param name="curve">曲线的 OID、坐标长度和摘要映射</param>
    /// <returns>PKCS#8 DER 字节</returns>
    private static byte[] EncodeEcPrivateKey(ECParameters parameters, CurveInfo curve)
    {
        byte[] point = EncodeEcPoint(parameters.Q, curve.CoordinateLength);
        // ECPrivateKey 同时保存私钥标量 D 和显式公钥点，确保 .NET 与 Web Crypto 均可导入
        byte[] privateKey = EncodeDerSequence(EncodeDerInteger(1), EncodeDer(0x04, PadLeft(parameters.D, curve.CoordinateLength)),
            EncodeDer(0xA1, EncodeDerBitString(point)));
        byte[] algorithm = EncodeDerSequence(EncodeDer(0x06, EC_PUBLIC_KEY_OID), EncodeDer(0x06, curve.ObjectIdentifier));
        return EncodeDerSequence(EncodeDerInteger(0), algorithm, EncodeDer(0x04, privateKey));
    }

    /// <summary>
    /// 把 EC 公钥参数编码为 SubjectPublicKeyInfo DER
    /// </summary>
    /// <param name="parameters">包含公钥点的 EC 参数</param>
    /// <param name="curve">曲线的 OID、坐标长度和摘要映射</param>
    /// <returns>SubjectPublicKeyInfo DER 字节</returns>
    private static byte[] EncodeEcPublicKey(ECParameters parameters, CurveInfo curve)
    {
        byte[] algorithm = EncodeDerSequence(EncodeDer(0x06, EC_PUBLIC_KEY_OID), EncodeDer(0x06, curve.ObjectIdentifier));
        return EncodeDerSequence(algorithm, EncodeDerBitString(EncodeEcPoint(parameters.Q, curve.CoordinateLength)));
    }

    /// <summary>
    /// 解析并验证 EC PKCS#8 PrivateKeyInfo DER
    /// </summary>
    /// <param name="value">PKCS#8 DER 字节</param>
    /// <param name="curve">调用方要求的曲线</param>
    /// <returns>包含私钥标量和未压缩公钥点的 EC 参数</returns>
    private static ECParameters ReadEcPrivateKey(byte[] value, CurveInfo curve)
    {
        DerReader outer = ReadSingleDerSequence(value);
        outer.ReadInteger();
        ReadAlgorithmIdentifier(outer, EC_PUBLIC_KEY_OID, curve.ObjectIdentifier);
        DerReader privateKey = ReadSingleDerSequence(outer.ReadValue(0x04));
        privateKey.ReadInteger();
        byte[] d = PadLeft(privateKey.ReadValue(0x04), curve.CoordinateLength);
        ECPoint point = default;
        bool hasPublicKey = false;
        while (privateKey.HasData)
        {
            byte tag = privateKey.PeekTag();
            if (tag == 0xA0)
            {
                // RFC 5915 允许私钥内部重复携带曲线参数；外层 PKCS#8 已完成曲线 OID 校验，因此跳过该可选字段
                privateKey.ReadValue(0xA0);
                continue;
            }

            if (tag != 0xA1)
            {
                throw new CryptographicException("EC PKCS#8 私钥包含不支持的字段。");
            }

            // [1] EXPLICIT BIT STRING 保存未压缩公钥点，导入 ECParameters 时 Q 和 D 必须属于同一曲线
            var publicKey = new DerReader(privateKey.ReadValue(0xA1));
            point = ReadEcPoint(publicKey.ReadBitString(), curve.CoordinateLength);
            publicKey.EnsureEmpty();
            hasPublicKey = true;
        }

        outer.EnsureEmpty();
        if (!hasPublicKey)
        {
            throw new CryptographicException("EC PKCS#8 私钥缺少互操作所需的公钥点。");
        }

        return new ECParameters {Curve = curve.Curve, D = d, Q = point};
    }

    /// <summary>
    /// 解析并验证 EC SubjectPublicKeyInfo DER
    /// </summary>
    /// <param name="value">SubjectPublicKeyInfo DER 字节</param>
    /// <param name="curve">调用方要求的曲线</param>
    /// <returns>包含未压缩公钥点的 EC 参数</returns>
    private static ECParameters ReadEcPublicKey(byte[] value, CurveInfo curve)
    {
        DerReader outer = ReadSingleDerSequence(value);
        ReadAlgorithmIdentifier(outer, EC_PUBLIC_KEY_OID, curve.ObjectIdentifier);
        ECPoint point = ReadEcPoint(outer.ReadBitString(), curve.CoordinateLength);
        outer.EnsureEmpty();
        return new ECParameters {Curve = curve.Curve, Q = point};
    }

    /// <summary>
    /// 读取必须只包含一个顶层 SEQUENCE 的 DER 值
    /// </summary>
    /// <param name="value">完整 DER 字节</param>
    /// <returns>限制在顶层 SEQUENCE 内容范围内的读取器</returns>
    private static DerReader ReadSingleDerSequence(byte[] value)
    {
        var reader = new DerReader(value);
        DerReader sequence = reader.ReadSequence();
        reader.EnsureEmpty();
        return sequence;
    }

    /// <summary>
    /// 读取并验证 ASN.1 AlgorithmIdentifier
    /// </summary>
    /// <param name="outer">包含 AlgorithmIdentifier 的外层读取器</param>
    /// <param name="expectedAlgorithm">预期算法 OID 的 DER 内容字节</param>
    /// <param name="expectedParameter">预期参数 OID；RSA 使用 <c>null</c> 表示可选 NULL</param>
    private static void ReadAlgorithmIdentifier(DerReader outer, byte[] expectedAlgorithm, byte[] expectedParameter)
    {
        DerReader algorithm = outer.ReadSequence();
        if (!CryptographicOperations.FixedTimeEquals(algorithm.ReadValue(0x06), expectedAlgorithm))
        {
            throw new CryptographicException("密钥算法标识与请求的算法不匹配。");
        }

        if (expectedParameter == null)
        {
            if (algorithm.HasData)
            {
                if (algorithm.ReadValue(0x05).Length != 0)
                {
                    throw new CryptographicException("DER NULL 参数长度无效。");
                }
            }
        }
        else if (!CryptographicOperations.FixedTimeEquals(algorithm.ReadValue(0x06), expectedParameter))
        {
            throw new CryptographicException("EC 密钥曲线与请求的曲线不匹配。");
        }

        algorithm.EnsureEmpty();
    }

    /// <summary>
    /// 按 SEC 1 未压缩点格式编码 EC 公钥点
    /// </summary>
    /// <param name="point">X/Y 仿射坐标</param>
    /// <param name="coordinateLength">曲线坐标的固定字节数</param>
    /// <returns><c>0x04 || X || Y</c> 格式的点</returns>
    private static byte[] EncodeEcPoint(ECPoint point, int coordinateLength)
    {
        byte[] x = PadLeft(point.X, coordinateLength);
        byte[] y = PadLeft(point.Y, coordinateLength);
        byte[] value = new byte[1 + coordinateLength * 2];
        value[0] = 0x04;
        Buffer.BlockCopy(x, 0, value, 1, coordinateLength);
        Buffer.BlockCopy(y, 0, value, 1 + coordinateLength, coordinateLength);
        return value;
    }

    /// <summary>
    /// 解析 SEC 1 未压缩 EC 公钥点
    /// </summary>
    /// <param name="value"><c>0x04 || X || Y</c> 格式的点</param>
    /// <param name="coordinateLength">曲线坐标的固定字节数</param>
    /// <returns>拆分后的 X/Y 仿射坐标</returns>
    private static ECPoint ReadEcPoint(byte[] value, int coordinateLength)
    {
        if (value.Length != 1 + coordinateLength * 2 || value[0] != 0x04)
        {
            throw new CryptographicException("仅支持未压缩格式的 NIST EC 公钥点。");
        }

        byte[] x = new byte[coordinateLength];
        byte[] y = new byte[coordinateLength];
        Buffer.BlockCopy(value, 1, x, 0, coordinateLength);
        Buffer.BlockCopy(value, 1 + coordinateLength, y, 0, coordinateLength);
        return new ECPoint {X = x, Y = y};
    }

    /// <summary>
    /// 把 Web Crypto 曲线名称映射到 .NET 曲线和协议参数
    /// </summary>
    /// <param name="namedCurve">P-256、P-384 或 P-521</param>
    /// <returns>曲线、OID、坐标长度和 ECDSA 摘要算法</returns>
    private static CurveInfo GetCurveInfo(string namedCurve)
    {
        switch (namedCurve)
        {
            case "P-256":
                return new CurveInfo(ECCurve.NamedCurves.nistP256, P256_OID, 32, HashAlgorithmName.SHA256);
            case "P-384":
                return new CurveInfo(ECCurve.NamedCurves.nistP384, P384_OID, 48, HashAlgorithmName.SHA384);
            case "P-521":
                return new CurveInfo(ECCurve.NamedCurves.nistP521, P521_OID, 66, HashAlgorithmName.SHA512);
            default:
                throw new ArgumentOutOfRangeException(nameof(namedCurve), "曲线必须是 P-256、P-384 或 P-521。");
        }
    }

    /// <summary>
    /// 把多个已编码 DER 字段组合为 SEQUENCE
    /// </summary>
    /// <param name="values">已包含标签和长度的 DER 字段</param>
    /// <returns>DER SEQUENCE</returns>
    private static byte[] EncodeDerSequence(params byte[][] values)
    {
        int contentLength = 0;
        foreach (byte[] value in values)
        {
            contentLength += value.Length;
        }

        byte[] content = new byte[contentLength];
        int offset = 0;
        foreach (byte[] value in values)
        {
            Buffer.BlockCopy(value, 0, content, offset, value.Length);
            offset += value.Length;
        }

        return EncodeDer(0x30, content);
    }

    /// <summary>
    /// 编码 0 至 127 范围内的非负 DER INTEGER
    /// </summary>
    /// <param name="value">要编码的小整数</param>
    /// <returns>DER INTEGER</returns>
    private static byte[] EncodeDerInteger(int value)
    {
        if (value < 0 || value > 127)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return EncodeDer(0x02, new[] {(byte)value});
    }

    /// <summary>
    /// 按最短正数形式编码无符号大整数
    /// </summary>
    /// <param name="value">大端无符号整数</param>
    /// <returns>带必要正号前导零的 DER INTEGER</returns>
    private static byte[] EncodeDerInteger(byte[] value)
    {
        if (value == null)
        {
            throw new CryptographicException("密钥参数不完整。");
        }

        int offset = 0;
        while (offset < value.Length && value[offset] == 0)
        {
            offset++;
        }

        byte[] normalized = new byte[value.Length - offset];
        Buffer.BlockCopy(value, offset, normalized, 0, normalized.Length);
        if (normalized.Length == 0)
        {
            normalized = new byte[] {0};
        }

        if ((normalized[0] & 0x80) != 0)
        {
            // DER INTEGER 是有符号数；最高位为 1 时补一个零字节，防止无符号参数被解释为负数
            byte[] positive = new byte[normalized.Length + 1];
            Buffer.BlockCopy(normalized, 0, positive, 1, normalized.Length);
            normalized = positive;
        }

        return EncodeDer(0x02, normalized);
    }

    /// <summary>
    /// 编码字节对齐、未使用位数为零的 DER BIT STRING
    /// </summary>
    /// <param name="value">位串内容字节</param>
    /// <returns>DER BIT STRING</returns>
    private static byte[] EncodeDerBitString(byte[] value)
    {
        byte[] content = new byte[value.Length + 1];
        Buffer.BlockCopy(value, 0, content, 1, value.Length);
        return EncodeDer(0x03, content);
    }

    /// <summary>
    /// 编码一个确定长度的 DER TLV 字段
    /// </summary>
    /// <param name="tag">单字节 ASN.1 标签</param>
    /// <param name="content">字段内容</param>
    /// <returns>标签、长度和内容拼接后的 DER 字段</returns>
    private static byte[] EncodeDer(int tag, byte[] content)
    {
        byte[] length;
        if (content.Length < 128)
        {
            length = new[] {(byte)content.Length};
        }
        else
        {
            // 长形式只编码表示长度所必需的非零高位字节，满足 DER 最短形式要求
            var lengthBytes = new List<byte>();
            int remaining = content.Length;
            while (remaining > 0)
            {
                lengthBytes.Insert(0, (byte)(remaining & 0xFF));
                remaining >>= 8;
            }

            lengthBytes.Insert(0, (byte)(0x80 | lengthBytes.Count));
            length = lengthBytes.ToArray();
        }

        byte[] result = new byte[1 + length.Length + content.Length];
        result[0] = (byte)tag;
        Buffer.BlockCopy(length, 0, result, 1, length.Length);
        Buffer.BlockCopy(content, 0, result, 1 + length.Length, content.Length);
        return result;
    }

    /// <summary>
    /// 移除无符号整数前导零并左侧补零到固定长度
    /// </summary>
    /// <param name="value">大端无符号整数</param>
    /// <param name="length">协议要求的固定字节数</param>
    /// <returns>固定长度的新数组</returns>
    private static byte[] PadLeft(byte[] value, int length)
    {
        if (value == null)
        {
            throw new CryptographicException("密钥参数不完整。");
        }

        int offset = 0;
        while (offset < value.Length && value[offset] == 0)
        {
            offset++;
        }

        byte[] normalized = new byte[value.Length - offset];
        Buffer.BlockCopy(value, offset, normalized, 0, normalized.Length);
        if (normalized.Length > length)
        {
            throw new CryptographicException("密钥参数长度无效。");
        }

        // EC 坐标和私钥标量是固定字段长度，左侧补零不会改变大端整数值
        byte[] result = new byte[length];
        Buffer.BlockCopy(normalized, 0, result, length - normalized.Length, normalized.Length);
        return result;
    }

    /// <summary>
    /// 统一保存 Web Crypto NIST 曲线在 .NET 和 ASN.1 中的映射
    /// </summary>
    private sealed class CurveInfo
    {
        /// <summary>
        /// 初始化不可变曲线映射
        /// </summary>
        /// <param name="curve">.NET 命名曲线</param>
        /// <param name="objectIdentifier">曲线 OID 的 DER 内容字节</param>
        /// <param name="coordinateLength">单个仿射坐标的固定字节数</param>
        /// <param name="hashAlgorithm">对应的 ECDSA 摘要算法</param>
        internal CurveInfo(ECCurve curve, byte[] objectIdentifier, int coordinateLength, HashAlgorithmName hashAlgorithm)
        {
            Curve = curve;
            ObjectIdentifier = objectIdentifier;
            CoordinateLength = coordinateLength;
            HashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        /// .NET 命名曲线
        /// </summary>
        internal ECCurve Curve { get; }

        /// <summary>
        /// 曲线 OID 的 DER 内容字节
        /// </summary>
        internal byte[] ObjectIdentifier { get; }

        /// <summary>
        /// 单个仿射坐标的固定字节数
        /// </summary>
        internal int CoordinateLength { get; }

        /// <summary>
        /// 该曲线默认使用的 ECDSA 摘要算法
        /// </summary>
        internal HashAlgorithmName HashAlgorithm { get; }
    }

    /// <summary>
    /// 只实现本文件 PKCS#8/SPKI 所需严格子集的 DER 读取器
    /// </summary>
    private sealed class DerReader
    {
        /// <summary>
        /// 当前读取器可访问的完整 DER 内容
        /// </summary>
        private readonly byte[] _value;

        /// <summary>
        /// 下一个标签相对于 <see cref="_value"/> 的偏移
        /// </summary>
        private int _offset;

        /// <summary>
        /// 为一个完整 DER 值或已切片的 SEQUENCE 内容创建读取器
        /// </summary>
        /// <param name="value">读取器独占访问范围内的字节</param>
        internal DerReader(byte[] value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// 获取当前范围内是否仍有未读取字节
        /// </summary>
        internal bool HasData => _offset < _value.Length;

        /// <summary>
        /// 读取但不移动当前位置的单字节标签
        /// </summary>
        /// <returns>当前位置的 ASN.1 标签</returns>
        internal byte PeekTag()
        {
            if (!HasData)
            {
                throw new CryptographicException("DER 数据意外结束。");
            }

            return _value[_offset];
        }

        /// <summary>
        /// 读取一个 DER SEQUENCE 并返回限制在其内容范围内的新读取器
        /// </summary>
        /// <returns>SEQUENCE 内容读取器</returns>
        internal DerReader ReadSequence()
        {
            return new DerReader(ReadValue(0x30));
        }

        /// <summary>
        /// 读取非负、最短编码的 DER INTEGER
        /// </summary>
        /// <returns>移除仅用于正号的前导零后的大端无符号整数</returns>
        internal byte[] ReadInteger()
        {
            byte[] value = ReadValue(0x02);
            if (value.Length == 0 || (value[0] & 0x80) != 0)
            {
                throw new CryptographicException("DER INTEGER 必须是非负整数。");
            }

            if (value.Length > 1 && value[0] == 0)
            {
                if ((value[1] & 0x80) == 0)
                {
                    throw new CryptographicException("DER INTEGER 不是最短编码。");
                }

                byte[] result = new byte[value.Length - 1];
                Buffer.BlockCopy(value, 1, result, 0, result.Length);
                return result;
            }

            return value;
        }

        /// <summary>
        /// 读取未使用位数为零的 DER BIT STRING
        /// </summary>
        /// <returns>不包含“未使用位数”前缀字节的内容</returns>
        internal byte[] ReadBitString()
        {
            byte[] value = ReadValue(0x03);
            if (value.Length == 0 || value[0] != 0)
            {
                throw new CryptographicException("仅支持字节对齐的 DER BIT STRING。");
            }

            byte[] result = new byte[value.Length - 1];
            Buffer.BlockCopy(value, 1, result, 0, result.Length);
            return result;
        }

        /// <summary>
        /// 读取具有指定单字节标签的 DER 字段
        /// </summary>
        /// <param name="expectedTag">当前位置必须出现的标签</param>
        /// <returns>不包含标签和长度的字段内容</returns>
        internal byte[] ReadValue(int expectedTag)
        {
            if (!HasData || _value[_offset++] != expectedTag)
            {
                throw new CryptographicException($"DER 数据缺少预期标签 0x{expectedTag:X2}。");
            }

            int length = ReadLength();
            if (length > _value.Length - _offset)
            {
                throw new CryptographicException("DER 字段长度超出输入范围。");
            }

            byte[] result = new byte[length];
            Buffer.BlockCopy(_value, _offset, result, 0, length);
            _offset += length;
            return result;
        }

        /// <summary>
        /// 确认当前读取范围已被完整消费
        /// </summary>
        internal void EnsureEmpty()
        {
            if (HasData)
            {
                throw new CryptographicException("DER 数据包含未预期的尾随字段。");
            }
        }

        /// <summary>
        /// 读取 DER 短形式或最短长形式长度
        /// </summary>
        /// <returns>当前字段的非负内容长度</returns>
        private int ReadLength()
        {
            if (!HasData)
            {
                throw new CryptographicException("DER 长度字段意外结束。");
            }

            byte first = _value[_offset++];
            if ((first & 0x80) == 0)
            {
                return first;
            }

            // DER 禁止 BER 的不定长度；本工具也拒绝超过 Int32 范围的超大字段
            int count = first & 0x7F;
            if (count == 0 || count > 4 || count > _value.Length - _offset)
            {
                throw new CryptographicException("DER 长度字段无效。");
            }

            int length = 0;
            for (int index = 0; index < count; index++)
            {
                length = checked((length << 8) | _value[_offset++]);
            }

            // 小于 128 的长度必须使用单字节短形式，拒绝存在多种表示的非规范输入
            if (length < 128)
            {
                throw new CryptographicException("DER 长度不是最短编码。");
            }

            return length;
        }
    }

    #endregion

    #region Random/Comparison

    /// <summary>
    /// 使用平台密码学随机数生成器生成安全随机字节
    /// </summary>
    /// <param name="length">要生成的字节数，范围为 0 至 65,536</param>
    /// <returns>具有指定长度的新字节数组</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> 超出允许范围</exception>
    public static byte[] GenerateRandomBytes(int length)
    {
        if (length < 0 || length > 65536)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "长度必须介于 0 和 65,536 之间。");
        }

        byte[] bytes = new byte[length];
        using var randomNumberGenerator = RandomNumberGenerator.Create();
        randomNumberGenerator.GetBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// 以不按内容提前退出的方式比较两个字节数组
    /// </summary>
    /// <param name="left">第一个字节数组</param>
    /// <param name="right">第二个字节数组</param>
    /// <returns>长度和所有字节均相同时返回 <c>true</c></returns>
    public static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        if (left == null)
        {
            throw new ArgumentNullException(nameof(left));
        }

        if (right == null)
        {
            throw new ArgumentNullException(nameof(right));
        }

        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    #endregion

    #region MD5

    /// <summary>
    /// 使用 MD5 算法计算字符串哈希
    /// </summary>
    /// <remarks>仅用于非安全的普通校验，不得用于密码存储、签名或抗碰撞场景</remarks>
    /// <param name="content">要处理的内容</param>
    /// <returns>使用 MD5 算法计算字符串哈希</returns>
    public static string MD5Encrypt(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        // 创建 MD5 实例
        using var mi = MD5.Create();

        // 将输入字符串转换为 UTF-8 字节
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

        // 计算原始摘要字节
        byte[] hashBytes = mi.ComputeHash(contentBytes);

        // BitConverter 生成带连字符的十六进制文本，移除分隔符后统一转换为小写
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    #endregion

    #region SHA1

    /// <summary>
    /// 计算 SHA-1 哈希
    /// </summary>
    /// <remarks>仅用于非安全的普通校验，不得用于密码存储、签名或抗碰撞场景</remarks>
    /// <param name="str">要处理的字符串</param>
    /// <returns>计算得到的 SHA-1 哈希</returns>
    public static string SHA1Encrypt(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        using var sha1 = SHA1.Create();
        byte[] contentBytes = Encoding.UTF8.GetBytes(str);
        byte[] hashBytes = sha1.ComputeHash(contentBytes);
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
    }

    #endregion

    #region SHA256

    /// <summary>
    /// 计算字符串的 SHA-256 哈希值
    /// </summary>
    /// <param name="content">要处理的内容</param>
    /// <returns>计算得到的字符串的 SHA-256 哈希值</returns>
    public static string SHA256Encrypt(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sha256 = SHA256.Create();
        return BitConverter.ToString(sha256.ComputeHash(contentBytes)).Replace("-", string.Empty);
    }

    /// <summary>
    /// 计算 UTF-8 字符串的 32 字节 SHA-256 摘要
    /// </summary>
    /// <param name="content">要计算摘要的 UTF-8 文本</param>
    /// <returns>32 字节 SHA-256 摘要</returns>
    public static byte[] SHA256Bytes(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(contentBytes);
    }

    #endregion

    #region SHA384

    /// <summary>
    /// 计算 UTF-8 字符串的 SHA-384 大写十六进制摘要
    /// </summary>
    /// <param name="content">要计算摘要的 UTF-8 文本</param>
    /// <returns>96 个大写十六进制字符组成的 SHA-384 摘要</returns>
    public static string SHA384Encrypt(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sha384 = SHA384.Create();
        return BitConverter.ToString(sha384.ComputeHash(contentBytes)).Replace("-", string.Empty);
    }

    /// <summary>
    /// 计算 UTF-8 字符串的 48 字节 SHA-384 摘要
    /// </summary>
    /// <param name="content">要计算摘要的 UTF-8 文本</param>
    /// <returns>48 字节 SHA-384 摘要</returns>
    public static byte[] SHA384Bytes(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sha384 = SHA384.Create();
        return sha384.ComputeHash(contentBytes);
    }

    #endregion

    #region SHA512

    /// <summary>
    /// 计算 UTF-8 字符串的 SHA-512 大写十六进制摘要
    /// </summary>
    /// <param name="content">要计算摘要的 UTF-8 文本</param>
    /// <returns>128 个大写十六进制字符组成的 SHA-512 摘要</returns>
    public static string SHA512Encrypt(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sha512 = SHA512.Create();
        return BitConverter.ToString(sha512.ComputeHash(contentBytes)).Replace("-", string.Empty);
    }

    /// <summary>
    /// 计算 UTF-8 字符串的 64 字节 SHA-512 摘要
    /// </summary>
    /// <param name="content">要计算摘要的 UTF-8 文本</param>
    /// <returns>64 字节 SHA-512 摘要</returns>
    public static byte[] SHA512Bytes(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
        using var sha512 = SHA512.Create();
        return sha512.ComputeHash(contentBytes);
    }

    #endregion

    #region HMAC

    /// <summary>
    /// 使用 HMAC-SHA-256 认证 UTF-8 文本
    /// </summary>
    /// <param name="content">要认证的 UTF-8 文本</param>
    /// <param name="key">非空的 UTF-8 HMAC 密钥</param>
    /// <returns>64 个小写十六进制字符组成的认证标签</returns>
    public static string HMACSHA256Encrypt(string content, string key)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // HMAC 直接使用调用方密钥的 UTF-8 字节，不再经过额外的密钥派生或截断
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length == 0)
        {
            throw new ArgumentException("HMAC 密钥不能为空。", nameof(key));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

        try
        {
            // 使用 HMAC-SHA-256 计算固定 32 字节认证标签
            using var hmac = new HMACSHA256(keyBytes);
            byte[] tagBytes = hmac.ComputeHash(contentBytes);

            // BitConverter 默认包含连字符，移除后统一输出小写十六进制
            return BitConverter.ToString(tagBytes).Replace("-", string.Empty).ToLowerInvariant();
        }
        finally
        {
            // 及时清除额外创建的密钥字节副本
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    /// <summary>
    /// 使用 HMAC-SHA-384 认证 UTF-8 文本
    /// </summary>
    /// <param name="content">要认证的 UTF-8 文本</param>
    /// <param name="key">非空的 UTF-8 HMAC 密钥</param>
    /// <returns>96 个小写十六进制字符组成的认证标签</returns>
    public static string HMACSHA384Encrypt(string content, string key)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // HMAC 直接使用调用方密钥的 UTF-8 字节，不再经过额外的密钥派生或截断
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length == 0)
        {
            throw new ArgumentException("HMAC 密钥不能为空。", nameof(key));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

        try
        {
            // 使用 HMAC-SHA-384 计算固定 48 字节认证标签
            using var hmac = new HMACSHA384(keyBytes);
            byte[] tagBytes = hmac.ComputeHash(contentBytes);

            // BitConverter 默认包含连字符，移除后统一输出小写十六进制
            return BitConverter.ToString(tagBytes).Replace("-", string.Empty).ToLowerInvariant();
        }
        finally
        {
            // 及时清除额外创建的密钥字节副本
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    /// <summary>
    /// 使用 HMAC-SHA-512 认证 UTF-8 文本
    /// </summary>
    /// <param name="content">要认证的 UTF-8 文本</param>
    /// <param name="key">非空的 UTF-8 HMAC 密钥</param>
    /// <returns>128 个小写十六进制字符组成的认证标签</returns>
    public static string HMACSHA512Encrypt(string content, string key)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        // HMAC 直接使用调用方密钥的 UTF-8 字节，不再经过额外的密钥派生或截断
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length == 0)
        {
            throw new ArgumentException("HMAC 密钥不能为空。", nameof(key));
        }

        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

        try
        {
            // 使用 HMAC-SHA-512 计算固定 64 字节认证标签
            using var hmac = new HMACSHA512(keyBytes);
            byte[] tagBytes = hmac.ComputeHash(contentBytes);

            // BitConverter 默认包含连字符，移除后统一输出小写十六进制
            return BitConverter.ToString(tagBytes).Replace("-", string.Empty).ToLowerInvariant();
        }
        finally
        {
            // 及时清除额外创建的密钥字节副本
            CryptographicOperations.ZeroMemory(keyBytes);
        }
    }

    #endregion

    #region PBKDF2

    /// <summary>
    /// 使用 PBKDF2-HMAC-SHA-256 从 UTF-8 密码派生指定长度的密钥
    /// </summary>
    /// <remarks>盐应由安全随机数生成器产生且至少 16 字节；派生结果不包含盐或迭代次数</remarks>
    /// <param name="password">1 至 1,024 UTF-8 字节的密码</param>
    /// <param name="salt">至少 8 字节的盐</param>
    /// <param name="iterations">PBKDF2 迭代次数，范围为 100,000 至 5,000,000</param>
    /// <param name="outputLength">派生密钥长度，范围为 1 至 1,024 字节</param>
    /// <returns>指定长度的派生密钥</returns>
    /// <exception cref="ArgumentOutOfRangeException">密码、盐、迭代次数或输出长度超出允许范围</exception>
    public static byte[] PBKDF2SHA256(string password, byte[] salt, int iterations = DEFAULT_PBKDF2_ITERATIONS,
        int outputLength = 32)
    {
        if (salt == null)
        {
            throw new ArgumentNullException(nameof(salt));
        }

        if (salt.Length < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(salt), "PBKDF2 盐不能少于 8 字节，推荐至少 16 字节。");
        }

        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        // PBKDF2 按 UTF-8 处理密码，并限制输入大小以避免异常参数占用过多内存
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        if (passwordBytes.Length == 0)
        {
            throw new ArgumentException("密码不能为空。", nameof(password));
        }

        if (passwordBytes.Length > MAXIMUM_PASSWORD_BYTES)
        {
            throw new ArgumentOutOfRangeException(nameof(password), $"UTF-8 密码不能超过 {MAXIMUM_PASSWORD_BYTES} 字节。");
        }

        if (iterations < MINIMUM_PBKDF2_ITERATIONS || iterations > MAXIMUM_PBKDF2_ITERATIONS)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations),
                $"PBKDF2 迭代次数必须介于 {MINIMUM_PBKDF2_ITERATIONS} 和 {MAXIMUM_PBKDF2_ITERATIONS} 之间。");
        }

        if (outputLength < 1 || outputLength > 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(outputLength), "输出长度必须介于 1 和 1,024 字节之间。");
        }

        try
        {
            // 显式指定 SHA-256，避免使用旧运行时的 PBKDF2 默认摘要算法
            using var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(outputLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    /// <summary>
    /// 使用随机盐生成可持久化的 PBKDF2-HMAC-SHA-256 密码哈希
    /// </summary>
    /// <param name="password">1 至 1,024 UTF-8 字节的密码</param>
    /// <param name="iterations">PBKDF2 迭代次数，范围为 100,000 至 5,000,000</param>
    /// <returns>包含版本、迭代次数、16 字节随机盐和 32 字节派生密钥的自描述字符串</returns>
    /// <exception cref="ArgumentOutOfRangeException">密码或迭代次数超出允许范围</exception>
    public static string HashPasswordPBKDF2SHA256(string password, int iterations = DEFAULT_PBKDF2_ITERATIONS)
    {
        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        // 密码哈希独立校验输入，不依赖通用 PBKDF2 入口的实现细节
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        if (passwordBytes.Length == 0)
        {
            throw new ArgumentException("密码不能为空。", nameof(password));
        }

        if (passwordBytes.Length > MAXIMUM_PASSWORD_BYTES)
        {
            throw new ArgumentOutOfRangeException(nameof(password), $"UTF-8 密码不能超过 {MAXIMUM_PASSWORD_BYTES} 字节。");
        }

        if (iterations < MINIMUM_PBKDF2_ITERATIONS || iterations > MAXIMUM_PBKDF2_ITERATIONS)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations),
                $"PBKDF2 迭代次数必须介于 {MINIMUM_PBKDF2_ITERATIONS} 和 {MAXIMUM_PBKDF2_ITERATIONS} 之间。");
        }

        // 每个密码独立生成盐，防止相同密码产生相同的持久化结果，并提高预计算攻击成本
        byte[] salt = new byte[16];
        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(salt);
        }

        byte[] derivedKey = null;
        try
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
            {
                // 密码哈希协议固定输出 32 字节派生密钥
                derivedKey = deriveBytes.GetBytes(32);
            }

            // 协议字段使用无填充 Base64Url，避免持久化文本包含路径或表单敏感字符
            string encodedSalt = Convert.ToBase64String(salt).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string encodedKey = Convert.ToBase64String(derivedKey).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return string.Join(":", PASSWORD_HASH_PREFIX, iterations.ToString(), encodedSalt, encodedKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            if (derivedKey != null)
            {
                CryptographicOperations.ZeroMemory(derivedKey);
            }
        }
    }

    /// <summary>
    /// 验证 <see cref="HashPasswordPBKDF2SHA256"/> 生成的密码哈希
    /// </summary>
    /// <param name="password">要验证的密码</param>
    /// <param name="passwordHash">自描述的 PBKDF2-HMAC-SHA-256 密码哈希</param>
    /// <returns>格式有效且密码匹配时返回 <c>true</c>；格式无效时返回 <c>false</c></returns>
    public static bool VerifyPasswordPBKDF2SHA256(string password, string passwordHash)
    {
        if (passwordHash == null)
        {
            throw new ArgumentNullException(nameof(passwordHash));
        }

        byte[] passwordBytes = null;
        byte[] expected = null;
        byte[] actual = null;
        try
        {
            // 先验证协议版本和字段数量，再执行高成本 PBKDF2
            string[] parts = passwordHash.Split(':');
            if (parts.Length != 4 || parts[0] != PASSWORD_HASH_PREFIX || !int.TryParse(parts[1], out int iterations))
            {
                return false;
            }

            if (iterations < MINIMUM_PBKDF2_ITERATIONS || iterations > MAXIMUM_PBKDF2_ITERATIONS)
            {
                return false;
            }

            // 严格解码盐字段，并拒绝填充、标准 Base64 字符和非规范尾部位
            string encodedSalt = parts[2];
            if (encodedSalt.IndexOf('=') >= 0
                || encodedSalt.IndexOf('+') >= 0
                || encodedSalt.IndexOf('/') >= 0
                || encodedSalt.Length % 4 == 1)
            {
                return false;
            }

            string normalizedSalt = encodedSalt.Replace('-', '+').Replace('_', '/');
            normalizedSalt = normalizedSalt.PadRight(normalizedSalt.Length + (4 - normalizedSalt.Length % 4) % 4, '=');
            byte[] salt = Convert.FromBase64String(normalizedSalt);
            string canonicalSalt = Convert.ToBase64String(salt).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (!string.Equals(canonicalSalt, encodedSalt, StringComparison.Ordinal))
            {
                return false;
            }

            // 对派生密钥字段执行相同的规范 Base64Url 校验
            string encodedKey = parts[3];
            if (encodedKey.IndexOf('=') >= 0
                || encodedKey.IndexOf('+') >= 0
                || encodedKey.IndexOf('/') >= 0
                || encodedKey.Length % 4 == 1)
            {
                return false;
            }

            string normalizedKey = encodedKey.Replace('-', '+').Replace('_', '/');
            normalizedKey = normalizedKey.PadRight(normalizedKey.Length + (4 - normalizedKey.Length % 4) % 4, '=');
            expected = Convert.FromBase64String(normalizedKey);
            string canonicalKey = Convert.ToBase64String(expected).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (!string.Equals(canonicalKey, encodedKey, StringComparison.Ordinal))
            {
                return false;
            }

            if (salt.Length != 16 || expected.Length != 32)
            {
                return false;
            }

            // 验证入口独立完成密码边界校验，避免依赖其他公共 PBKDF2 方法
            if (password == null)
            {
                return false;
            }

            passwordBytes = Encoding.UTF8.GetBytes(password);
            if (passwordBytes.Length == 0 || passwordBytes.Length > MAXIMUM_PASSWORD_BYTES)
            {
                return false;
            }

            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
            {
                actual = deriveBytes.GetBytes(expected.Length);
            }

            // 固定时间比较完整派生结果，不按首个差异位置提前退出
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        finally
        {
            if (passwordBytes != null)
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }

            if (actual != null)
            {
                CryptographicOperations.ZeroMemory(actual);
            }

            if (expected != null)
            {
                CryptographicOperations.ZeroMemory(expected);
            }
        }
    }

    #endregion

    #region HKDF

    /// <summary>
    /// 使用 RFC 5869 HKDF-SHA-256 派生上下文隔离的密钥材料
    /// </summary>
    /// <param name="inputKeyMaterial">输入密钥材料，例如 ECDH 原始共享秘密</param>
    /// <param name="salt">可选盐；传入 <c>null</c> 时使用 32 个零字节</param>
    /// <param name="info">可选的应用和用途上下文</param>
    /// <param name="outputLength">输出长度，1 至 8,160 字节</param>
    /// <returns>指定长度、与 <paramref name="info"/> 上下文绑定的派生密钥</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputLength"/> 超出 RFC 5869 限制</exception>
    public static byte[] HKDFSHA256(byte[] inputKeyMaterial, byte[] salt = null, byte[] info = null, int outputLength = 32)
    {
        if (inputKeyMaterial == null)
        {
            throw new ArgumentNullException(nameof(inputKeyMaterial));
        }

        if (outputLength < 1 || outputLength > 255 * 32)
        {
            throw new ArgumentOutOfRangeException(nameof(outputLength), "输出长度必须介于 1 和 8,160 字节之间。");
        }

        byte[] actualSalt = salt == null || salt.Length == 0 ? new byte[32] : salt;
        byte[] actualInfo = info ?? Array.Empty<byte>();
        byte[] pseudorandomKey;
        // Extract 阶段先把任意长度输入压缩成固定 32 字节伪随机密钥
        using (var extract = new HMACSHA256(actualSalt))
        {
            pseudorandomKey = extract.ComputeHash(inputKeyMaterial);
        }

        try
        {
            byte[] output = new byte[outputLength];
            byte[] previous = Array.Empty<byte>();
            int offset = 0;
            int blockIndex = 1;
            // Expand 阶段按 T(n) = HMAC(PRK, T(n-1) || info || n) 逐块生成输出
            while (offset < output.Length)
            {
                byte[] blockInput = new byte[previous.Length + actualInfo.Length + 1];
                Buffer.BlockCopy(previous, 0, blockInput, 0, previous.Length);
                Buffer.BlockCopy(actualInfo, 0, blockInput, previous.Length, actualInfo.Length);
                blockInput[blockInput.Length - 1] = (byte)blockIndex;
                using var expand = new HMACSHA256(pseudorandomKey);
                previous = expand.ComputeHash(blockInput);
                int copyLength = Math.Min(previous.Length, output.Length - offset);
                Buffer.BlockCopy(previous, 0, output, offset, copyLength);
                offset += copyLength;
                blockIndex++;
            }

            CryptographicOperations.ZeroMemory(previous);
            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pseudorandomKey);
        }
    }

    #endregion

    #region AES

    /// <summary>
    /// 使用 AES 算法对给定字符串进行加密
    /// </summary>
    /// <remarks>同一密钥重复使用固定 IV 会泄露明文模式；调用方应为不同数据提供不可预测且不重复的 IV</remarks>
    /// <param name="dataStr">要解析或处理的数据文本</param>
    /// <param name="key">用于加密的密钥。必须 32 位</param>
    /// <param name="vector">对称加密使用的初始化向量</param>
    /// <param name="cipherMode">对称加密使用的密码块模式</param>
    /// <param name="paddingMode">对称加密使用的填充模式</param>
    /// <returns>使用 AES 算法对给定字符串进行加密</returns>
    public static string AESEncrypt(string dataStr, string key, string vector, CipherMode cipherMode = CipherMode.CBC,
        PaddingMode paddingMode = PaddingMode.PKCS7)
    {
        if (string.IsNullOrWhiteSpace(dataStr))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(vector))
        {
            return null;
        }

        // AES 密钥不足 32 个字符时使用 f 补齐
        if (key.Length < 32)
        {
            key = key.PadRight(32, 'f');
        }

        // AES 密钥超过 32 个字符时截断
        if (key.Length > 32)
        {
            key = key[..32];
        }

        // AES 初始化向量不足 16 个字符时使用 f 补齐
        if (vector.Length < 16)
        {
            vector = vector.PadRight(16, 'f');
        }

        // AES 初始化向量超过 16 个字符时截断
        if (vector.Length > 16)
        {
            vector = vector[..16];
        }

        // 将输入的字符串、密钥和向量转换为字节数组
        byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] vectorBytes = Encoding.UTF8.GetBytes(vector);

        // 创建 AES 实例并设置加密模式和填充模式
        using var aesAlg = Aes.Create();
        aesAlg.Mode = cipherMode;
        aesAlg.Padding = paddingMode;

        // 创建加密器对象，并使用密钥和向量初始化
        using ICryptoTransform encryption = aesAlg.CreateEncryptor(keyBytes, vectorBytes);

        // 创建内存流和加密流，将加密数据写入加密流
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryption, CryptoStreamMode.Write, true);
        csEncrypt.Write(dataBytes, 0, dataBytes.Length);
        csEncrypt.FlushFinalBlock();

        // 获取加密后的字节数组并转换为 Base64 编码字符串
        byte[] array = msEncrypt.ToArray();
        return Convert.ToBase64String(array);
    }

    /// <summary>
    /// 使用 AES 算法对给定的 Base64 编码字符串进行解密
    /// </summary>
    /// <param name="dataStr">要解析或处理的数据文本</param>
    /// <param name="key">用于解密的密钥。必须 32 位</param>
    /// <param name="vector">对称加密使用的初始化向量</param>
    /// <param name="cipherMode">对称加密使用的密码块模式</param>
    /// <param name="paddingMode">对称加密使用的填充模式</param>
    /// <returns>使用 AES 算法对给定的 Base64 编码字符串进行解密</returns>
    public static string AESDecrypt(string dataStr, string key, string vector, CipherMode cipherMode = CipherMode.CBC,
        PaddingMode paddingMode = PaddingMode.PKCS7)
    {
        if (string.IsNullOrWhiteSpace(dataStr))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(vector))
        {
            return null;
        }

        // AES 密钥不足 32 个字符时使用 f 补齐
        if (key.Length < 32)
        {
            key = key.PadRight(32, 'f');
        }

        // AES 密钥超过 32 个字符时截断
        if (key.Length > 32)
        {
            key = key[..32];
        }

        // AES 初始化向量不足 16 个字符时使用 f 补齐
        if (vector.Length < 16)
        {
            vector = vector.PadRight(16, 'f');
        }

        // AES 初始化向量超过 16 个字符时截断
        if (vector.Length > 16)
        {
            vector = vector[..16];
        }

        // 将输入的 Base64 字符串、密钥和向量转换为字节数组
        byte[] dataBytes = Convert.FromBase64String(dataStr);
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] vectorBytes = Encoding.UTF8.GetBytes(vector);

        // 创建 AES 实例并设置解密模式和填充模式
        using var aesAlg = Aes.Create();
        aesAlg.Mode = cipherMode;
        aesAlg.Padding = paddingMode;

        // 创建解密器对象，并使用密钥和向量初始化
        using ICryptoTransform decryption = aesAlg.CreateDecryptor(keyBytes, vectorBytes);

        // 创建内存流和解密流，将解密数据写入解密流
        using var msDecryption = new MemoryStream(dataBytes);
        using var csDecryption = new CryptoStream(msDecryption, decryption, CryptoStreamMode.Read);
        using var srDecryption = new StreamReader(csDecryption);
        return srDecryption.ReadToEnd();
    }

    /// <summary>
    /// 使用 AES-GCM 加密并认证字符串
    /// </summary>
    /// <remarks>此方法同时提供机密性和完整性；CBC/ECB 不具备认证能力</remarks>
    /// <param name="dataStr">要解析或处理的数据文本</param>
    /// <param name="key">密钥材料；内部使用 SHA-256 归一化为 256 位密钥</param>
    /// <returns>包含格式版本、随机 nonce、认证标签和密文的 Base64 字符串</returns>
    public static string AESEncryptAuthenticated(string dataStr, string key)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("密钥不能为空。", nameof(key));
        }

        const byte formatVersion = 1;
        const int nonceLength = 12;
        const int tagLength = 16;

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(dataStr);
        byte[] keyMaterialBytes = Encoding.UTF8.GetBytes(key);
        byte[] keyBytes;
        using (var sha256 = SHA256.Create())
        {
            keyBytes = sha256.ComputeHash(keyMaterialBytes);
        }

        byte[] nonce = new byte[nonceLength];
        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(nonce);
        }

        byte[] tag = new byte[tagLength];
        byte[] ciphertext = new byte[plaintextBytes.Length];

        try
        {
#if NET8_0_OR_GREATER
            using var aesGcm = new AesGcm(keyBytes, tagLength);
#else
            using var aesGcm = new AesGcm(keyBytes);
#endif
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(keyMaterialBytes);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }

        // 格式：[1 字节版本][12 字节随机 nonce][16 字节认证标签][密文]
        byte[] payload = new byte[1 + nonceLength + tagLength + ciphertext.Length];
        payload[0] = formatVersion;
        Buffer.BlockCopy(nonce, 0, payload, 1, nonceLength);
        Buffer.BlockCopy(tag, 0, payload, 1 + nonceLength, tagLength);
        Buffer.BlockCopy(ciphertext, 0, payload, 1 + nonceLength + tagLength, ciphertext.Length);
        return Convert.ToBase64String(payload);
    }

    /// <summary>
    /// 解密并验证 <see cref="AESEncryptAuthenticated"/> 生成的字符串
    /// </summary>
    /// <exception cref="CryptographicException">密钥错误、密文被篡改或格式不受支持</exception>
    /// <param name="dataStr">要解析或处理的数据文本</param>
    /// <param name="key">加密时使用的密钥材料</param>
    /// <returns>解密并验证 AESEncryptAuthenticated 生成的字符串</returns>
    public static string AESDecryptAuthenticated(string dataStr, string key)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("密钥不能为空。", nameof(key));
        }

        const byte supportedVersion = 1;
        const int nonceLength = 12;
        const int tagLength = 16;

        byte[] payload = Convert.FromBase64String(dataStr);
        if (payload.Length < 1 + nonceLength + tagLength || payload[0] != supportedVersion)
        {
            throw new CryptographicException("AES-GCM 密文格式无效或版本不受支持。");
        }

        byte[] nonce = payload.AsSpan(1, nonceLength).ToArray();
        byte[] tag = payload.AsSpan(1 + nonceLength, tagLength).ToArray();
        byte[] ciphertext = payload.AsSpan(1 + nonceLength + tagLength).ToArray();
        byte[] plaintext = new byte[ciphertext.Length];
        byte[] keyMaterialBytes = Encoding.UTF8.GetBytes(key);
        byte[] keyBytes;
        using (var sha256 = SHA256.Create())
        {
            keyBytes = sha256.ComputeHash(keyMaterialBytes);
        }

        try
        {
#if NET8_0_OR_GREATER
            using var aesGcm = new AesGcm(keyBytes, tagLength);
#else
            using var aesGcm = new AesGcm(keyBytes);
#endif
            // 认证失败时 AesGcm.Decrypt 会抛出 CryptographicException，不返回未经验证的明文
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(keyMaterialBytes);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// 使用 PBKDF2-HMAC-SHA-256 派生 AES-256-GCM 密钥并认证加密 UTF-8 文本
    /// </summary>
    /// <remarks>
    /// 每次调用生成独立的 16 字节盐和 12 字节 nonce，结果与 TypeScript 工具的
    /// <c>aesEncryptWithPassword</c> 格式完全一致
    /// </remarks>
    /// <param name="dataStr">UTF-8 编码后不超过 8 MiB 的明文</param>
    /// <param name="password">1 至 1,024 UTF-8 字节的密码</param>
    /// <param name="iterations">PBKDF2 迭代次数，默认 600,000</param>
    /// <returns>带版本、迭代次数、盐、nonce、密文和认证标签的自描述字符串</returns>
    public static string AESEncryptWithPassword(string dataStr, string password, int iterations = DEFAULT_PBKDF2_ITERATIONS)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        // 此入口独立校验并编码密码，避免依赖其他 PBKDF2 公共方法的行为
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        if (passwordBytes.Length == 0)
        {
            throw new ArgumentException("密码不能为空。", nameof(password));
        }

        if (passwordBytes.Length > MAXIMUM_PASSWORD_BYTES)
        {
            throw new ArgumentOutOfRangeException(nameof(password), $"UTF-8 密码不能超过 {MAXIMUM_PASSWORD_BYTES} 字节。");
        }

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(dataStr);
        if (plaintextBytes.Length > MAXIMUM_PLAINTEXT_BYTES)
        {
            throw new ArgumentOutOfRangeException(nameof(dataStr), $"UTF-8 明文不能超过 {MAXIMUM_PLAINTEXT_BYTES} 字节。");
        }

        if (iterations < MINIMUM_PBKDF2_ITERATIONS || iterations > MAXIMUM_PBKDF2_ITERATIONS)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations),
                $"PBKDF2 迭代次数必须介于 {MINIMUM_PBKDF2_ITERATIONS} 和 {MAXIMUM_PBKDF2_ITERATIONS} 之间。");
        }

        // 盐保证相同密码不会派生相同密钥；nonce 保证同一派生密钥下的每次 GCM 加密都具有唯一输入
        byte[] salt = new byte[16];
        byte[] nonce = new byte[12];
        using (var randomNumberGenerator = RandomNumberGenerator.Create())
        {
            randomNumberGenerator.GetBytes(salt);
            randomNumberGenerator.GetBytes(nonce);
        }

        byte[] keyBytes;
        using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
        {
            // AES-256 固定使用 32 字节派生密钥
            keyBytes = deriveBytes.GetBytes(32);
        }

        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[16];
        byte[] additionalDataBytes = Encoding.UTF8.GetBytes(PASSWORD_ENCRYPTION_PREFIX);

        try
        {
#if NET8_0_OR_GREATER
            using var aesGcm = new AesGcm(keyBytes, tag.Length);
#else
            using var aesGcm = new AesGcm(keyBytes);
#endif
            // 固定版本前缀同时作为 AAD，使攻击者无法在不破坏认证标签的情况下替换协议版本
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag, additionalDataBytes);

            // Web Crypto 把认证标签追加在密文后，因此这里采用相同顺序以保证 TypeScript/.NET 互操作
            byte[] ciphertextAndTag = new byte[ciphertext.Length + tag.Length];
            Buffer.BlockCopy(ciphertext, 0, ciphertextAndTag, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, ciphertextAndTag, ciphertext.Length, tag.Length);

            // 每个二进制字段独立编码为无填充 Base64Url，与 TypeScript 协议保持一致
            string encodedSalt = Convert.ToBase64String(salt).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string encodedNonce = Convert.ToBase64String(nonce).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            string encodedCiphertext = Convert.ToBase64String(ciphertextAndTag).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return string.Join(":", PASSWORD_ENCRYPTION_PREFIX, iterations.ToString(), encodedSalt, encodedNonce,
                encodedCiphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    /// <summary>
    /// 解密并认证 <see cref="AESEncryptWithPassword"/> 生成的跨语言载荷
    /// </summary>
    /// <param name="payload">带版本、迭代次数、盐、nonce、密文和认证标签的载荷</param>
    /// <param name="password">加密时使用的密码</param>
    /// <returns>通过认证后的原始 UTF-8 明文</returns>
    /// <exception cref="ArgumentOutOfRangeException">载荷或密码超过支持的长度</exception>
    /// <exception cref="CryptographicException">密码错误、载荷被篡改或格式无效</exception>
    public static string AESDecryptWithPassword(string payload, string password)
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (payload.Length > MAXIMUM_PAYLOAD_LENGTH)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "加密载荷超过支持的大小。");
        }

        if (password == null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        // 解密入口独立校验密码字节边界，不依赖其他 PBKDF2 或密码哈希方法
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        if (passwordBytes.Length == 0)
        {
            throw new ArgumentException("密码不能为空。", nameof(password));
        }

        if (passwordBytes.Length > MAXIMUM_PASSWORD_BYTES)
        {
            throw new ArgumentOutOfRangeException(nameof(password), $"UTF-8 密码不能超过 {MAXIMUM_PASSWORD_BYTES} 字节。");
        }

        byte[] keyBytes = null;
        byte[] plaintext = null;
        try
        {
            // 在执行高成本 PBKDF2 前先验证固定字段数、协议版本和迭代次数字段
            string[] parts = payload.Split(':');
            if (parts.Length != 5 || parts[0] != PASSWORD_ENCRYPTION_PREFIX || !int.TryParse(parts[1], out int iterations))
            {
                throw new CryptographicException("AES-GCM 密码载荷格式无效或版本不受支持。");
            }

            if (iterations < MINIMUM_PBKDF2_ITERATIONS || iterations > MAXIMUM_PBKDF2_ITERATIONS)
            {
                throw new ArgumentOutOfRangeException(nameof(iterations),
                    $"PBKDF2 迭代次数必须介于 {MINIMUM_PBKDF2_ITERATIONS} 和 {MAXIMUM_PBKDF2_ITERATIONS} 之间。");
            }

            // 严格解码盐字段，并校验其无填充 Base64Url 表示是否规范
            string encodedSalt = parts[2];
            if (encodedSalt.IndexOf('=') >= 0
                || encodedSalt.IndexOf('+') >= 0
                || encodedSalt.IndexOf('/') >= 0
                || encodedSalt.Length % 4 == 1)
            {
                throw new FormatException("盐字段不是有效的 Base64Url。");
            }

            string normalizedSalt = encodedSalt.Replace('-', '+').Replace('_', '/');
            normalizedSalt = normalizedSalt.PadRight(normalizedSalt.Length + (4 - normalizedSalt.Length % 4) % 4, '=');
            byte[] salt = Convert.FromBase64String(normalizedSalt);
            string canonicalSalt = Convert.ToBase64String(salt).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (!string.Equals(canonicalSalt, encodedSalt, StringComparison.Ordinal))
            {
                throw new FormatException("盐字段不是规范的 Base64Url 编码。");
            }

            // nonce 与盐使用相同的严格 Base64Url 解析规则
            string encodedNonce = parts[3];
            if (encodedNonce.IndexOf('=') >= 0
                || encodedNonce.IndexOf('+') >= 0
                || encodedNonce.IndexOf('/') >= 0
                || encodedNonce.Length % 4 == 1)
            {
                throw new FormatException("nonce 字段不是有效的 Base64Url。");
            }

            string normalizedNonce = encodedNonce.Replace('-', '+').Replace('_', '/');
            normalizedNonce = normalizedNonce.PadRight(normalizedNonce.Length + (4 - normalizedNonce.Length % 4) % 4, '=');
            byte[] nonce = Convert.FromBase64String(normalizedNonce);
            string canonicalNonce = Convert.ToBase64String(nonce).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            if (!string.Equals(canonicalNonce, encodedNonce, StringComparison.Ordinal))
            {
                throw new FormatException("nonce 字段不是规范的 Base64Url 编码。");
            }

            // 密文和认证标签作为一个字段传输，解码后再按固定 16 字节标签拆分
            string encodedCiphertext = parts[4];
            if (encodedCiphertext.IndexOf('=') >= 0
                || encodedCiphertext.IndexOf('+') >= 0
                || encodedCiphertext.IndexOf('/') >= 0
                || encodedCiphertext.Length % 4 == 1)
            {
                throw new FormatException("密文字段不是有效的 Base64Url。");
            }

            string normalizedCiphertext = encodedCiphertext.Replace('-', '+').Replace('_', '/');
            normalizedCiphertext = normalizedCiphertext.PadRight(
                normalizedCiphertext.Length + (4 - normalizedCiphertext.Length % 4) % 4, '=');
            byte[] ciphertextAndTag = Convert.FromBase64String(normalizedCiphertext);
            string canonicalCiphertext = Convert
                .ToBase64String(ciphertextAndTag)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            if (!string.Equals(canonicalCiphertext, encodedCiphertext, StringComparison.Ordinal))
            {
                throw new FormatException("密文字段不是规范的 Base64Url 编码。");
            }

            if (salt.Length != 16 || nonce.Length != 12 || ciphertextAndTag.Length < 16)
            {
                throw new CryptographicException("AES-GCM 密码载荷字段长度无效。");
            }

            int ciphertextLength = ciphertextAndTag.Length - 16;
            byte[] ciphertext = new byte[ciphertextLength];
            byte[] tag = new byte[16];
            // TypeScript Web Crypto 返回 ciphertext || tag；拆分后交给 .NET AesGcm 的独立参数
            Buffer.BlockCopy(ciphertextAndTag, 0, ciphertext, 0, ciphertextLength);
            Buffer.BlockCopy(ciphertextAndTag, ciphertextLength, tag, 0, tag.Length);
            plaintext = new byte[ciphertextLength];
            using (var deriveBytes = new Rfc2898DeriveBytes(passwordBytes, salt, iterations, HashAlgorithmName.SHA256))
            {
                // AES-256 固定使用 32 字节派生密钥
                keyBytes = deriveBytes.GetBytes(32);
            }

            byte[] additionalDataBytes = Encoding.UTF8.GetBytes(PASSWORD_ENCRYPTION_PREFIX);

#if NET8_0_OR_GREATER
            using var aesGcm = new AesGcm(keyBytes, tag.Length);
#else
            using var aesGcm = new AesGcm(keyBytes);
#endif
            // 只有密码、AAD、nonce、密文和标签全部正确时，Decrypt 才会返回明文
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, additionalDataBytes);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("AES-GCM 密码载荷包含无效的 Base64Url 字段。", exception);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new CryptographicException("AES-GCM 密码载荷参数无效。", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            if (keyBytes != null)
            {
                CryptographicOperations.ZeroMemory(keyBytes);
            }

            if (plaintext != null)
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
    }

    #endregion

    #region RSA

    /// <summary>
    /// 生成可供 .NET 与 Web Crypto 互操作的 RSA PKCS#8/SPKI PEM 密钥对
    /// </summary>
    /// <param name="modulusLength">RSA 模数位数，默认 2,048；必须至少为 2,048 且是 256 的倍数</param>
    /// <returns>未加密 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥组成的 PEM 密钥对</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="modulusLength"/> 不符合安全边界</exception>
    public static PemKeyPair GenerateRSAKeyPair(int modulusLength = 2048)
    {
        if (modulusLength < 2048 || modulusLength % 256 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(modulusLength), "RSA 模数必须至少为 2,048 位且是 256 的倍数。");
        }

        using var rsa = RSA.Create();
        rsa.KeySize = modulusLength;
        RSAParameters parameters = rsa.ExportParameters(true);
        // 显式编码 PKCS#8/SPKI，避免 netstandard2.1 缺少 PEM 便捷 API，并保持与 Web Crypto 的格式一致
        return new PemKeyPair(ToPem("PRIVATE KEY", EncodeRsaPrivateKey(parameters)),
            ToPem("PUBLIC KEY", EncodeRsaPublicKey(parameters)));
    }

    /// <summary>
    /// 使用 RSA-OAEP/SHA-256 公钥加密 UTF-8 文本
    /// </summary>
    /// <param name="dataStr">要加密的 UTF-8 文本；长度必须满足 RSA-OAEP 模数限制</param>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 公钥</param>
    /// <returns>Base64 编码的 RSA 密文</returns>
    /// <exception cref="CryptographicException">公钥格式无效或明文超过 RSA-OAEP 容量</exception>
    public static string RSAEncryptOAEP(string dataStr, string publicKeyPem)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
        using var rsa = RSA.Create();
        rsa.ImportParameters(ReadRsaPublicKey(FromPem(publicKeyPem, "PUBLIC KEY")));
        return Convert.ToBase64String(rsa.Encrypt(dataBytes, RSAEncryptionPadding.OaepSHA256));
    }

    /// <summary>
    /// 使用 RSA-OAEP/SHA-256 私钥解密 Base64 密文
    /// </summary>
    /// <param name="dataStr">Base64 编码的 RSA 密文</param>
    /// <param name="privateKeyPem">未加密的 PKCS#8 PEM 私钥</param>
    /// <returns>解密后的 UTF-8 文本</returns>
    /// <exception cref="FormatException"><paramref name="dataStr"/> 不是有效 Base64</exception>
    /// <exception cref="CryptographicException">私钥格式无效、密钥不匹配或密文损坏</exception>
    public static string RSADecryptOAEP(string dataStr, string privateKeyPem)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        using var rsa = RSA.Create();
        rsa.ImportParameters(ReadRsaPrivateKey(FromPem(privateKeyPem, "PRIVATE KEY")));
        return Encoding.UTF8.GetString(rsa.Decrypt(Convert.FromBase64String(dataStr), RSAEncryptionPadding.OaepSHA256));
    }

    /// <summary>
    /// 使用 RSA-PSS/SHA-256 私钥签名 UTF-8 文本
    /// </summary>
    /// <param name="dataStr">要签名的 UTF-8 文本</param>
    /// <param name="privateKeyPem">未加密的 PKCS#8 PEM 私钥</param>
    /// <returns>Base64 编码的 RSA-PSS 签名；PSS 盐长度等于 SHA-256 摘要长度</returns>
    /// <exception cref="CryptographicException">私钥格式无效或签名失败</exception>
    public static string RSASignPSS(string dataStr, string privateKeyPem)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
        using var rsa = RSA.Create();
        rsa.ImportParameters(ReadRsaPrivateKey(FromPem(privateKeyPem, "PRIVATE KEY")));
        return Convert.ToBase64String(rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
    }

    /// <summary>
    /// 使用 RSA-PSS/SHA-256 公钥验证 Base64 签名
    /// </summary>
    /// <param name="dataStr">签名时使用的 UTF-8 文本</param>
    /// <param name="signature">Base64 编码的 RSA-PSS 签名</param>
    /// <param name="publicKeyPem">SubjectPublicKeyInfo PEM 公钥</param>
    /// <returns>签名格式有效且与内容、公钥匹配时返回 <c>true</c></returns>
    /// <exception cref="FormatException"><paramref name="signature"/> 不是有效 Base64</exception>
    /// <exception cref="CryptographicException">公钥格式无效</exception>
    public static bool RSAVerifyPSS(string dataStr, string signature, string publicKeyPem)
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        if (signature == null)
        {
            throw new ArgumentNullException(nameof(signature));
        }

        byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
        byte[] signatureBytes = Convert.FromBase64String(signature);
        using var rsa = RSA.Create();
        rsa.ImportParameters(ReadRsaPublicKey(FromPem(publicKeyPem, "PUBLIC KEY")));
        return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    #endregion

    #region ECDSA

    /// <summary>
    /// 生成 ECDSA PKCS#8/SPKI PEM 签名密钥对
    /// </summary>
    /// <param name="namedCurve">NIST 曲线名称：P-256、P-384 或 P-521</param>
    /// <returns>未加密 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥组成的 PEM 密钥对</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="namedCurve"/> 不受支持</exception>
    public static PemKeyPair GenerateECDSAKeyPair(string namedCurve = "P-256")
    {
        CurveInfo curve = GetCurveInfo(namedCurve);
        using var ecdsa = ECDsa.Create(curve.Curve);
        ECParameters parameters = ecdsa.ExportParameters(true);
        return new PemKeyPair(ToPem("PRIVATE KEY", EncodeEcPrivateKey(parameters, curve)),
            ToPem("PUBLIC KEY", EncodeEcPublicKey(parameters, curve)));
    }

    /// <summary>
    /// 使用 ECDSA 和曲线对应的 SHA-2 摘要签名 UTF-8 文本
    /// </summary>
    /// <remarks>签名使用 IEEE P1363 固定字段拼接格式，与 Web Crypto 一致</remarks>
    /// <param name="dataStr">要签名的 UTF-8 文本</param>
    /// <param name="privateKeyPem">未加密的 EC PKCS#8 PEM 私钥</param>
    /// <param name="namedCurve">私钥使用的 NIST 曲线</param>
    /// <returns>Base64 编码的 IEEE P1363 ECDSA 签名</returns>
    /// <exception cref="CryptographicException">私钥格式或曲线不匹配</exception>
    public static string ECDSASign(string dataStr, string privateKeyPem, string namedCurve = "P-256")
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
        CurveInfo curve = GetCurveInfo(namedCurve);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportParameters(ReadEcPrivateKey(FromPem(privateKeyPem, "PRIVATE KEY"), curve));
        return Convert.ToBase64String(ecdsa.SignData(dataBytes, curve.HashAlgorithm));
    }

    /// <summary>
    /// 验证 Web Crypto 兼容的 ECDSA IEEE P1363 Base64 签名
    /// </summary>
    /// <param name="dataStr">签名时使用的 UTF-8 文本</param>
    /// <param name="signature">Base64 编码的 IEEE P1363 ECDSA 签名</param>
    /// <param name="publicKeyPem">EC SubjectPublicKeyInfo PEM 公钥</param>
    /// <param name="namedCurve">公钥使用的 NIST 曲线</param>
    /// <returns>签名与内容、公钥和曲线匹配时返回 <c>true</c></returns>
    /// <exception cref="FormatException"><paramref name="signature"/> 不是有效 Base64</exception>
    /// <exception cref="CryptographicException">公钥格式或曲线不匹配</exception>
    public static bool ECDSAVerify(string dataStr, string signature, string publicKeyPem, string namedCurve = "P-256")
    {
        if (dataStr == null)
        {
            throw new ArgumentNullException(nameof(dataStr));
        }

        if (signature == null)
        {
            throw new ArgumentNullException(nameof(signature));
        }

        byte[] dataBytes = Encoding.UTF8.GetBytes(dataStr);
        byte[] signatureBytes = Convert.FromBase64String(signature);
        CurveInfo curve = GetCurveInfo(namedCurve);
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportParameters(ReadEcPublicKey(FromPem(publicKeyPem, "PUBLIC KEY"), curve));
        return ecdsa.VerifyData(dataBytes, signatureBytes, curve.HashAlgorithm);
    }

    #endregion

    #region ECDH

    /// <summary>
    /// 生成 ECDH PKCS#8/SPKI PEM 密钥协商密钥对
    /// </summary>
    /// <param name="namedCurve">NIST 曲线名称：P-256、P-384 或 P-521</param>
    /// <returns>未加密 PKCS#8 私钥和 SubjectPublicKeyInfo 公钥组成的 PEM 密钥对</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="namedCurve"/> 不受支持</exception>
    public static PemKeyPair GenerateECDHKeyPair(string namedCurve = "P-256")
    {
        CurveInfo curve = GetCurveInfo(namedCurve);
        using var ecdh = ECDiffieHellman.Create(curve.Curve);
        ECParameters parameters = ecdh.ExportParameters(true);
        return new PemKeyPair(ToPem("PRIVATE KEY", EncodeEcPrivateKey(parameters, curve)),
            ToPem("PUBLIC KEY", EncodeEcPublicKey(parameters, curve)));
    }

    /// <summary>
    /// 派生 ECDH 原始共享秘密
    /// </summary>
    /// <remarks>原始秘密必须继续输入 HKDF 等 KDF，不得直接作为长期密钥</remarks>
    /// <param name="privateKeyPem">本方未加密的 EC PKCS#8 PEM 私钥</param>
    /// <param name="publicKeyPem">对方的 EC SubjectPublicKeyInfo PEM 公钥</param>
    /// <param name="namedCurve">双方密钥使用的 NIST 曲线</param>
    /// <returns>曲线字段长度的原始 ECDH 共享秘密</returns>
    /// <exception cref="CryptographicException">密钥格式、曲线或密钥协商无效</exception>
    /// <exception cref="PlatformNotSupportedException">运行时不支持原始 ECDH 密钥协商</exception>
    public static byte[] DeriveECDHSecret(string privateKeyPem, string publicKeyPem, string namedCurve = "P-256")
    {
        CurveInfo curve = GetCurveInfo(namedCurve);
        using var privateKey = ECDiffieHellman.Create();
        using var publicKey = ECDiffieHellman.Create();
        privateKey.ImportParameters(ReadEcPrivateKey(FromPem(privateKeyPem, "PRIVATE KEY"), curve));
        publicKey.ImportParameters(ReadEcPublicKey(FromPem(publicKeyPem, "PUBLIC KEY"), curve));

        MethodInfo method = typeof(ECDiffieHellman).GetMethod("DeriveRawSecretAgreement",
            BindingFlags.Instance | BindingFlags.Public, null, new[] {typeof(ECDiffieHellmanPublicKey)}, null);
        if (method == null)
        {
            throw new PlatformNotSupportedException("当前运行时不支持原始 ECDH 共享秘密。");
        }

        try
        {
            // netstandard2.1 参考程序集没有该 API，因此在支持它的现代运行时上通过反射调用
            return (byte[])method.Invoke(privateKey, new object[] {publicKey.PublicKey});
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    /// <summary>
    /// 使用 ECDH 后以 SHA-256 派生 32 字节共享密钥
    /// </summary>
    /// <remarks>此方法直接调用平台 ECDH SHA-256 KDF，优先于直接使用原始共享秘密</remarks>
    /// <param name="privateKeyPem">本方未加密的 EC PKCS#8 PEM 私钥</param>
    /// <param name="publicKeyPem">对方的 EC SubjectPublicKeyInfo PEM 公钥</param>
    /// <param name="namedCurve">双方密钥使用的 NIST 曲线</param>
    /// <returns>32 字节共享密钥</returns>
    /// <exception cref="CryptographicException">密钥格式、曲线或密钥协商无效</exception>
    public static byte[] DeriveECDHKeySHA256(string privateKeyPem, string publicKeyPem, string namedCurve = "P-256")
    {
        CurveInfo curve = GetCurveInfo(namedCurve);
        using var privateKey = ECDiffieHellman.Create();
        using var publicKey = ECDiffieHellman.Create();
        privateKey.ImportParameters(ReadEcPrivateKey(FromPem(privateKeyPem, "PRIVATE KEY"), curve));
        publicKey.ImportParameters(ReadEcPublicKey(FromPem(publicKeyPem, "PUBLIC KEY"), curve));
        return privateKey.DeriveKeyFromHash(publicKey.PublicKey, HashAlgorithmName.SHA256);
    }

    #endregion
}
