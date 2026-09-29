// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Text;
using System.Text.RegularExpressions;

namespace Fast.Logging;

/// <summary>
/// 模板静态类
/// </summary>
internal static class TP
{
    static TP()
    {
        // 处理不同编码问题
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// 模板正则表达式对象
    /// </summary>
    private static readonly Lazy<Regex> _lazyRegex = new(() => new Regex(@"^##(?<prop>.*)?##[:：]?\s*(?<content>[\s\S]*)"));

    /// <summary>
    /// 生成规范日志模板
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="description">描述</param>
    /// <param name="items">列表项，如果以 ##xxx## 开头，自动生成 xxx: 属性</param>
    /// <returns>带标题、描述和列表项的文本日志模板</returns>
    public static string Wrapper(string title, string description, params string[] items)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"┏━━━━━━━━━━━  {title} ━━━━━━━━━━━").AppendLine();

        // 添加描述
        if (!string.IsNullOrWhiteSpace(description))
        {
            stringBuilder.Append($"┣ {description}").AppendLine().Append("┣ ").AppendLine();
        }

        // 添加项
        if (items != null && items.Length > 0)
        {
            int propMaxLength = items.Where(u => _lazyRegex.Value.IsMatch(u))
                .DefaultIfEmpty(string.Empty)
                .Max(u => _lazyRegex.Value.Match(u).Groups["prop"].Value.Length);

            // 控制项名称对齐空白占位数
            propMaxLength += propMaxLength >= 5 ? 10 : 5;

            // 遍历每一项并进行正则表达式匹配
            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i];

                // 判断是否匹配 ##xxx##
                if (_lazyRegex.Value.IsMatch(item))
                {
                    Match match = _lazyRegex.Value.Match(item);
                    string prop = match.Groups["prop"].Value;
                    string content = match.Groups["content"].Value;

                    string propTitle = $"{prop}：";
                    stringBuilder.Append($"┣ {PadRight(propTitle, propMaxLength)}{content}").AppendLine();
                }
                else
                {
                    stringBuilder.Append($"┣ {item}").AppendLine();
                }
            }
        }

        stringBuilder.Append($"┗━━━━━━━━━━━  {title} ━━━━━━━━━━━").AppendLine();
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 矩形包裹
    /// </summary>
    /// <param name="lines">多行消息</param>
    /// <param name="align">对齐方式，-1/左对齐；0/居中对其；1/右对齐</param>
    /// <param name="pad">间隙</param>
    /// <returns>按指定宽度和对齐方式绘制的矩形文本</returns>
    public static string WrapperRectangle(string[] lines, int align = 0, int pad = 20)
    {
        // 计算矩形框的宽度，取所有字符串中最长的长度，再乘以 2
        int width = lines.Max(GetLength) + pad;

        // 创建一个 StringBuilder 对象
        var stringBuilder = new StringBuilder();

        // 在 StringBuilder 对象中添加矩形框的上边框
        stringBuilder.AppendLine("+" + new string('-', width - 2) + "+");

        // 遍历每个字符串，并添加到 StringBuilder 对象中
        foreach (string line in lines)
        {
            // 当前字符串的长度
            int len = GetLength(line);
            int padding = align switch
            {
                -1 => 2,
                0 => (width - len - 2) / 2,
                1 => width - len - 2 - 2,
                _ => 2
            };

            // 在 StringBuilder 对象中添加当前字符串前的空格，使得当前字符串在矩形框中居中显示
            stringBuilder.Append("|" + new string(' ', padding));

            // 在 StringBuilder 对象中添加当前字符串
            stringBuilder.Append(line);

            // 在 StringBuilder 对象中添加当前字符串后的空格，使得矩形框的宽度保持不变
            stringBuilder.Append(new string(' ', width - len - 2 - padding) + "|");

            // 在 StringBuilder 对象中添加换行符
            stringBuilder.AppendLine();

            // 更新当前行数
        }

        // 在 StringBuilder 对象中添加矩形框的下边框
        stringBuilder.Append("+" + new string('-', width - 2) + "+");

        // 返回包含矩形框的所有字符串的 StringBuilder 对象的字符串表示形式
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 等宽文字对齐
    /// </summary>
    /// <param name="str">要处理的字符串</param>
    /// <param name="totalByteCount">填充后应达到的总字节数</param>
    /// <returns>等宽文字对齐</returns>
    private static string PadRight(string str, int totalByteCount)
    {
        var coding = Encoding.GetEncoding("gbk");
        int dcount = 0;

        foreach (char character in str)
        {
            if (coding.GetByteCount(character.ToString()) == 2)
            {
                dcount++;
            }
        }

        string w = str.PadRight(totalByteCount - dcount);
        return w;
    }

    /// <summary>
    /// 获取字符串长度
    /// </summary>
    /// <param name="str">字符串</param>
    /// <returns>字符串长度</returns>
    public static int GetLength(string str)
    {
        var coding = Encoding.GetEncoding("gbk");
        return coding.GetByteCount(str);
    }
}
