// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fast.Serialization;

/// <summary>
/// Enum 类型 JSON 返回处理
/// </summary>
internal sealed class EnumJsonConverter : JsonConverter
{
    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
        }
        else
        {
            // 获取枚举类型
            Type enumType = value.GetType();
            Type underlyingType = Nullable.GetUnderlyingType(enumType) ?? enumType;

            // 按枚举底层类型的 TypeCode 分派数值转换逻辑
            TypeCode typeCode = Type.GetTypeCode(underlyingType);

            // 判断是否为 long 类型
            if (typeCode == TypeCode.Int64)
            {
                writer.WriteValue($"{Convert.ToInt64(value)}");
            }
            else
            {
                writer.WriteValue(value);
            }
        }
    }

    /// <inheritdoc />
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        // 前端传入的 Enum 类型可能为 String 类型，或者 Number 类型
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        Type underlyingType = Nullable.GetUnderlyingType(objectType) ?? objectType;

        if (reader.TokenType == JsonToken.String)
        {
            var jToken = JToken.ReadFrom(reader);
            string enumValueStr = jToken.Value<string>();
            if (Enum.TryParse(underlyingType, enumValueStr, out object enumValue))
            {
                return enumValue;
            }
        }
        else if (reader.TokenType == JsonToken.Integer)
        {
            // 按枚举底层类型的 TypeCode 分派数值转换逻辑
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.SByte:
                    {
                        sbyte value = Convert.ToSByte(reader.Value);

                        sbyte flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToSByte(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.Byte:
                    {
                        byte value = Convert.ToByte(reader.Value);

                        byte flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToByte(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.Int16:
                    {
                        short value = Convert.ToInt16(reader.Value);

                        short flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToInt16(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.UInt16:
                    {
                        ushort value = Convert.ToUInt16(reader.Value);

                        ushort flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToUInt16(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.Int32:
                    {
                        int value = Convert.ToInt32(reader.Value);

                        int flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToInt32(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.UInt32:
                    {
                        uint value = Convert.ToUInt32(reader.Value);

                        uint flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToUInt32(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.Int64:
                    {
                        long value = Convert.ToInt64(reader.Value);

                        long flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToInt64(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.UInt64:
                    {
                        ulong value = Convert.ToUInt64(reader.Value);

                        ulong flagValues = 0;
                        foreach (object enumValue in Enum.GetValues(underlyingType))
                        {
                            flagValues |= Convert.ToUInt64(enumValue);
                        }

                        if ((value & flagValues) == value)
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }

                        // 数值不是已定义的枚举成员且为 0 时，按 null 处理
                        if (value == 0)
                        {
                            return null;
                        }
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        bool value = Convert.ToBoolean(reader.Value);
                        if (Enum.IsDefined(underlyingType, value))
                        {
                            return Enum.ToObject(underlyingType, value);
                        }
                    }
                    break;
            }
        }

        throw new JsonSerializationException($"Unable to convert JSON value to Enum {objectType}");
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return Nullable.GetUnderlyingType(objectType)?.IsEnum == true;
        }

        return objectType.IsEnum;
    }
}
