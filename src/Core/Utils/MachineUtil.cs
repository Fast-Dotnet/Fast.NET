// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Fast.NET.Core;

/// <summary>
/// 系统机器工具类
/// </summary>
[SuppressSniffer]
public static class MachineUtil
{
    /// <summary>
    /// 是否为 Unix/Linux 操作系统
    /// </summary>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsUnix()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    /// <summary>
    /// 是否为 MacOS 操作系统
    /// </summary>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsMacOS()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    /// <summary>
    /// 获取当前操作系统的版本描述
    /// </summary>
    /// <returns>macOS 或 Linux 的发行版本；Windows 返回运行时提供的操作系统描述</returns>
    public static string GetOSDescription()
    {
        if (IsMacOS())
        {
            // 使用 sw_vers 命令获取 MacOS 的版本信息，并提取操作系统版本号
            string output = ShellUtil.Bash("sw_vers | awk 'NR<=2{printf \"%s \", $NF}'");
            if (output != null)
            {
                // 去除百分号并返回版本信息
                return output.Replace("%", string.Empty);
            }

            return string.Empty;
        }

        if (IsUnix())
        {
            // 使用 /etc/os-release 文件中的 VERSION_ID 获取 Linux 发行版的版本号
            string output = ShellUtil.Bash("awk -F= '/^VERSION_ID/ {print $2}' /etc/os-release | tr -d '\"'");
            return output ?? string.Empty;
        }

        // Windows
        return RuntimeInformation.OSDescription;
    }

    /// <summary>
    /// 获取系统启动时间
    /// </summary>
    /// <returns>获取到的系统启动时间</returns>
    public static DateTime GetSystemStartTime()
    {
        if (IsMacOS())
        {
            // MacOS 获取系统启动时间：sysctl -n kern.boottime | awk '{print $4}' | tr -d ','
            // 返回：1705379131
            string output = ShellUtil
                .Bash("date -r $(sysctl -n kern.boottime | awk '{print $4}' | tr -d ',') +\"%Y-%m-%d %H:%M:%S\"")
                .Trim();
            return DateTime.Parse(output, CultureInfo.InvariantCulture);
        }

        if (IsUnix())
        {
            // 使用 awk 命令来获取 Linux 系统的 uptime 信息
            string output = ShellUtil.Bash("date -d \"$(awk -F. '{print $1}' /proc/uptime) second ago\" +\"%Y-%m-%d %H:%M:%S\"")
                .Trim();
            return DateTime.Parse(output, CultureInfo.InvariantCulture);
        }
        // Windows
        else
        {
            string output;
            try
            {
                // 使用 wmic 获取系统启动时间
                output = ShellUtil.Cmd("wmic", "OS get LastBootUpTime/Value");
            }
            catch (Win32Exception)
            {
                // 使用 PowerShell 查询系统启动时间
                output = ShellUtil.Cmd("powershell",
                    "-NoProfile -Command (Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToString('yyyyMMddHHmmss')");
            }

            string timeValue = output.Replace("LastBootUpTime=", string.Empty)
                .Trim()
                .Split('.', StringSplitOptions.RemoveEmptyEntries)[0];

            return DateTime.ParseExact(timeValue, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }
    }

    /// <summary>
    /// 获取系统运行时间描述
    /// </summary>
    /// <param name="format">输出格式化，默认：“00 天 00 时 00 分 00 秒”</param>
    /// <returns>获取到的系统运行时间描述</returns>
    public static string GetSystemRunTimes(string format = "dd\\ \\天\\ hh\\ \\时\\ mm\\ \\分\\ ss\\ \\秒")
    {
        DateTime dateTime = DateTime.Now;

        DateTime startTime = GetSystemStartTime();

        TimeSpan diffTime = dateTime - startTime;

        return diffTime.ToString(format);
    }

    /// <summary>
    /// 获取当前进程启动时间
    /// </summary>
    /// <returns>获取到的当前进程启动时间</returns>
    public static DateTime GetProgramStartTime()
    {
        try
        {
            return Process.GetCurrentProcess().StartTime;
        }
        catch (NotSupportedException)
        {
            // 在某些受限的 Linux 容器环境中，Process.StartTime 可能不受支持
            return DateTime.Now;
        }
    }

    /// <summary>
    /// 获取当前进程运行时间描述
    /// </summary>
    /// <param name="format">输出格式化，默认：“00 天 00 时 00 分 00 秒”</param>
    /// <returns>获取到的当前进程运行时间描述</returns>
    public static string GetProgramRunTimes(string format = "dd\\ \\天\\ hh\\ \\时\\ mm\\ \\分\\ ss\\ \\秒")
    {
        DateTime dateTime = DateTime.Now;

        DateTime startTime = GetProgramStartTime();

        TimeSpan diffTime = dateTime - startTime;

        return diffTime.ToString(format);
    }

    /// <summary>
    /// 获取操作系统 CPU 使用率
    /// </summary>
    /// <returns>获取到的操作系统 CPU 使用率集合</returns>
    public static List<decimal> GetSystemCpuRate()
    {
        var rates = new List<decimal>();

        if (IsMacOS())
        {
            // 使用 top 命令获取获取 CPU 使用率（用户和系统占用总和）
            string output = ShellUtil.Bash("top -l 1 | grep \"CPU usage\" | awk '{print $3 + $5}'");
            rates.Add(decimal.Parse(output, CultureInfo.InvariantCulture));
        }
        else if (IsUnix())
        {
            // 通过解析 '/proc/stat' 文件来计算 CPU 使用率
            string output = ShellUtil.Bash(
                "awk '{u=$2+$4; t=$2+$4+$5; if (NR==1){u1=u; t1=t;} else print ($2+$4-u1) * 100 / (t-t1); }' <(grep 'cpu ' /proc/stat) <(sleep 1;grep 'cpu ' /proc/stat)");
            rates.Add(decimal.Parse(output, CultureInfo.InvariantCulture));
        }
        // Windows
        else
        {
            string output;
            try
            {
                // 使用 wmic 获取 CPU 使用率
                output = ShellUtil.Cmd("wmic", "cpu get LoadPercentage");
            }
            catch (Win32Exception)
            {
                // 使用 powershell 获取 CPU 使用率
                output = ShellUtil.Cmd("powershell",
                    "-NoProfile -Command Get-CimInstance Win32_Processor | ForEach-Object { $_.LoadPercentage }");
            }

            rates.AddRange(output.Replace("LoadPercentage", string.Empty)
                .Trim()
                .Split(["\r", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(sl =>
                {
                    if (string.IsNullOrWhiteSpace(sl.Trim()))
                    {
                        return 0;
                    }

                    return decimal.Parse(sl.Trim(), CultureInfo.InvariantCulture);
                }));
        }

        return rates;
    }

    /// <summary>
    /// 在指定采样间隔内计算当前进程的 CPU 使用率
    /// </summary>
    /// <param name="sleep">采样间隔，单位为毫秒；默认值为 500</param>
    /// <returns>表示异步采样的任务，任务结果为按逻辑处理器数量归一化后的 CPU 使用百分比</returns>
    public static async Task<decimal> GetProgramCpuUsage(int sleep = 500)
    {
        // 获取当前进程对象
        var process = Process.GetCurrentProcess();

        DateTime startTime = DateTime.UtcNow;
        TimeSpan startUsage;
        try
        {
            startUsage = process.TotalProcessorTime;
        }
        catch (NotSupportedException)
        {
            // 在某些受限的 Linux 容器环境中，TotalProcessorTime 可能不受支持
            return 0;
        }

        await Task.Delay(sleep);

        process.Refresh();

        DateTime endTime = DateTime.UtcNow;
        TimeSpan endUsage;
        try
        {
            endUsage = process.TotalProcessorTime;
        }
        catch (NotSupportedException)
        {
            // 在某些受限的 Linux 容器环境中，TotalProcessorTime 可能不受支持
            return 0;
        }

        // 计算在延迟期间 CPU 使用的时间（单位：微秒）
        double usedMs = (endUsage - startUsage).TotalMilliseconds;
        double totalMs = (endTime - startTime).TotalMilliseconds;

        if (totalMs <= 0 || usedMs <= 0)
        {
            return 0;
        }

        // 考虑多核，计算总 CPU 时间的比例
        decimal usageTotal = (decimal)(usedMs / (Environment.ProcessorCount * totalMs) * 100);

        // 四舍五入保留两位小数
        return Math.Round(usageTotal, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// 获取操作系统内存信息，单位(MB)
    /// </summary>
    /// <returns>total：总内存 used：已用内存 free：可用内存</returns>
    public static (decimal total, decimal used, decimal free) GetSystemRamInfo()
    {
        decimal total = 0;
        decimal used = 0;
        decimal free = 0;

        if (IsMacOS())
        {
            // 获取总内存：sysctl 命令返回的值为字节，转换为 MB
            string output1 = ShellUtil.Bash("sysctl -n hw.memsize | awk '{printf \"%.2f\", $1/1024/1024}'");
            total = decimal.Parse(output1.Replace("%", string.Empty), CultureInfo.InvariantCulture);

            // 获取已用内存：top 命令中显示物理内存的使用情况，PhysMem 返回可用内存和已用内存的合计，单位为 KB
            string output2 = ShellUtil.Bash("top -l 1 -s 0 | awk '/PhysMem/ {print $6+$8}'");
            free = decimal.Parse(output2, CultureInfo.InvariantCulture);

            used = total - free;
        }
        else if (IsUnix())
        {
            // 使用 `awk` 命令从 `/proc/meminfo` 获取总内存和可用内存，单位为 KB
            string output = ShellUtil.Bash(
                "awk '/MemTotal/ {total=$2} /MemAvailable/ {available=$2} END {print total,available}' /proc/meminfo");
            string[] memory = output.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (memory.Length == 2)
            {
                // 解析总内存，已用内存，可用内存
                total = decimal.Parse(memory[0], CultureInfo.InvariantCulture) / 1024;
                free = decimal.Parse(memory[1], CultureInfo.InvariantCulture) / 1024;

                used = total - free;
            }
        }
        // Windows
        else
        {
            string output;
            try
            {
                // 使用 `wmic` 命令获取内存信息
                output = ShellUtil.Cmd("wmic", "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value");
            }
            catch (Win32Exception)
            {
                // 使用 powershell 命令获取内存信息
                output = ShellUtil.Cmd("powershell",
                    "-NoProfile -Command (Get-CimInstance Win32_OperatingSystem | Select-Object -ExpandProperty FreePhysicalMemory).ToString() + ',' + (Get-CimInstance Win32_OperatingSystem | Select-Object -ExpandProperty TotalVisibleMemorySize).ToString()");
            }

            string[] lines = output.Trim().Split([",", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

            // 提取并解析内存信息：总内存和可用内存（单位：KB）
            string[] freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            string[] totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            total = decimal.Parse(totalMemoryParts.Length > 1 ? totalMemoryParts[1] : totalMemoryParts[0],
                        CultureInfo.InvariantCulture)
                    / 1024;
            free = decimal.Parse(freeMemoryParts.Length > 1 ? freeMemoryParts[1] : freeMemoryParts[0],
                       CultureInfo.InvariantCulture)
                   / 1024;

            used = total - free;
        }

        return (total, used, free);
    }

    /// <summary>
    /// 获取当前进程内存信息，单位(MB)
    /// </summary>
    /// <returns>working：RAM 物理内存 peakWorking：最大 RAM 物理内存 virtualMemory：虚拟内存 peakVirtualMemory：最大虚拟内存 pagedMemory：分页内存 peakPagedMemory：最大分页内存</returns>
    public static (decimal working, decimal peakWorking, decimal virtualMemory, decimal peakVirtualMemory, decimal pagedMemory,
        decimal peakPagedMemory) GetProgramMemoryInfo()
    {
        // RAM 物理内存
        decimal working = 0;
        // 最大 RAM 物理内存
        decimal peakWorking = 0;
        decimal virtualMemory = 0;
        decimal peakVirtualMemory = 0;
        // 分页内存
        decimal pagedMemory = 0;
        // 最大分页内存
        decimal peakPagedMemory = 0;

        if (IsMacOS())
        {
            // 本地获取内存的方法可能需要额外的库或调用系统 API
        }
        else if (IsUnix())
        {
            decimal ByteToMB(string line)
            {
                // 解析文件中的内存值，VmRSS: 123456 kB
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    return 0;
                }

                decimal value = Convert.ToDecimal(parts[1]);
                string unit = parts[2].ToLower();

                return unit switch
                {
                    "kb" => value / 1024,
                    "mb" => value,
                    "gb" => value * 1024,
                    _ => value
                };
            }

            // 过读取 /proc/self/status 文件获取 Linux 系统内存信息
            string[] lines = File.ReadAllLines("/proc/self/status");

            foreach (string line in lines)
            {
                // 物理内存
                if (line.StartsWith("VmRSS:"))
                {
                    working = ByteToMB(line);
                }
                // 最大物理内存（高水位线）
                else if (line.StartsWith("VmHWM:"))
                {
                    peakWorking = ByteToMB(line);
                }
                else if (line.StartsWith("VmSize:"))
                {
                    virtualMemory = ByteToMB(line);
                }
                else if (line.StartsWith("VmPeak:"))
                {
                    peakVirtualMemory = ByteToMB(line);
                }
                // 分页内存（交换内存）
                else if (line.StartsWith("VmSwap:"))
                {
                    pagedMemory = ByteToMB(line);
                    // Linux 中没有 类似 PeakPagedMemory 的字段，所以这里直接返回交换内存
                    peakPagedMemory = pagedMemory;
                }
            }
        }
        // Windows
        else
        {
            // 获取当前进程对象
            var process = Process.GetCurrentProcess();

            const decimal relation = 1024 * 1024;

            working = process.WorkingSet64 / relation;
            peakWorking = process.PeakWorkingSet64 / relation;
            virtualMemory = process.VirtualMemorySize64 / relation;
            peakVirtualMemory = process.PeakVirtualMemorySize64 / relation;
            pagedMemory = process.PagedMemorySize64 / relation;
            peakPagedMemory = process.PeakPagedMemorySize64 / relation;
        }

        return (working, peakWorking, virtualMemory, peakVirtualMemory, pagedMemory, peakPagedMemory);
    }

    /// <summary>
    /// 获取硬盘信息
    /// </summary>
    /// <returns>获取到的硬盘信息集合</returns>
    public static List<DiskInfo> GetDiskInfos()
    {
        var diskInfos = new List<DiskInfo>();

        if (IsMacOS())
        {
            string output = ShellUtil.Bash(@"df -m | awk '/^\/dev\/disk/ {print $1,$2,$3,$4,$5}'");
            string[] disks = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (disks.Length < 1)
            {
                return diskInfos;
            }

            foreach (string item in disks)
            {
                string[] disk = item.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (disk.Length >= 5)
                {
                    var diskInfo = new DiskInfo
                    {
                        DiskName = disk[0],
                        TypeName =
                            ShellUtil.Bash("diskutil info " + disk[0] + " | awk '/File System Personality/ {print $4}'")
                                .Replace("\n", string.Empty),
                        TotalSize = Math.Round(long.Parse(disk[1]) / 1024M, 2, MidpointRounding.AwayFromZero),
                        Used = Math.Round(long.Parse(disk[2]) / 1024M, 2, MidpointRounding.AwayFromZero),
                        AvailableFreeSpace = Math.Round(long.Parse(disk[3]) / 1024M, 2, MidpointRounding.AwayFromZero),
                        AvailablePercent = decimal.Parse(disk[4].Replace("%", ""), CultureInfo.InvariantCulture)
                    };
                    diskInfos.Add(diskInfo);
                }
            }
        }
        else if (IsUnix())
        {
            string output = ShellUtil.Bash(@"df -mT | awk '/^\/dev\/(sd|vd|xvd|nvme|sda|vda|mapper)/ {print $1,$2,$3,$4,$5,$6}'");
            string[] disks = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (disks.Length >= 1)
            {
                foreach (string item in disks)
                {
                    string[] disk = item.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (disk.Length < 6)
                    {
                        continue;
                    }

                    var diskInfo = new DiskInfo
                    {
                        DiskName = disk[0],
                        TypeName = disk[1],
                        TotalSize = Math.Round(long.Parse(disk[2]) / 1024M, 2, MidpointRounding.AwayFromZero),
                        Used = Math.Round(long.Parse(disk[3]) / 1024M, 2, MidpointRounding.AwayFromZero),
                        AvailableFreeSpace = Math.Round(long.Parse(disk[4]) / 1024M, 2, MidpointRounding.AwayFromZero),
                        AvailablePercent = decimal.Parse(disk[5].Replace("%", ""), CultureInfo.InvariantCulture)
                    };
                    diskInfos.Add(diskInfo);
                }
            }
        }
        // Windows
        else
        {
            IEnumerable<DriveInfo> driveList = DriveInfo.GetDrives().Where(u => u.IsReady);

            const decimal relation = 1024 * 1024 * 1024;

            foreach (DriveInfo item in driveList)
            {
                if (item.DriveType == DriveType.CDRom)
                {
                    continue;
                }

                var diskInfo = new DiskInfo
                {
                    DiskName = item.Name,
                    TypeName = item.DriveType.ToString(),
                    TotalSize = Math.Round(item.TotalSize / relation, 2, MidpointRounding.AwayFromZero),
                    AvailableFreeSpace = Math.Round(item.AvailableFreeSpace / relation, 2, MidpointRounding.AwayFromZero)
                };
                diskInfo.Used = diskInfo.TotalSize - diskInfo.AvailableFreeSpace;
                diskInfo.AvailablePercent =
                    Math.Round(diskInfo.Used / diskInfo.TotalSize * 100, 2, MidpointRounding.AwayFromZero);
                diskInfos.Add(diskInfo);
            }
        }

        return diskInfos;
    }
}
