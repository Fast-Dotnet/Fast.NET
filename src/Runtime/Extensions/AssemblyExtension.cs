// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Fast.Runtime;

/// <summary>
/// 为 <see cref="Assembly"/> 提供扩展方法
/// </summary>
public static class AssemblyExtension
{
    /// <summary>
    /// 获取入口运行库
    /// </summary>
    /// <remarks>暂不支持独立/单文件发布</remarks>
    /// <param name="assembly">目标程序集</param>
    /// <returns>获取到的入口运行库集合</returns>
    public static List<DependencyLibrary> GetEntryRuntimeLibraries(this Assembly assembly)
    {
        string depsJsonFilePath = ResolveDependencyContextPath(assembly);
        if (!string.IsNullOrWhiteSpace(depsJsonFilePath))
        {
            // 读取文件
            string depsJsonContent = File.ReadAllText(depsJsonFilePath);

            // 解析 JSON 字符串
            JsonElement depsJsonRoot = JsonDocument.Parse(depsJsonContent).RootElement;

            var targetsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 获取 "targets" 节点的值
            JsonElement.ObjectEnumerator targetsContent = depsJsonRoot.GetProperty("targets").EnumerateObject();
            foreach (JsonProperty targetsArr in targetsContent)
            {
                // "targets" 节点下通常有一个节点，例如 ".NETCoreApp,Version=v8.0"
                foreach (JsonProperty targets in targetsArr.Value.EnumerateObject())
                {
                    if (targets.Value.TryGetProperty("runtime", out JsonElement runtimeElement))
                    {
                        // 直接默认获取第一个（大多数包只有一个主程序集）
                        JsonProperty runtimeObj = runtimeElement.EnumerateObject().FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(runtimeObj.Name))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(runtimeObj.Name);
                            if (!string.IsNullOrWhiteSpace(fileName))
                            {
                                targetsMap.TryAdd(targets.Name, fileName);
                            }
                        }
                    }
                }
            }

            // 获取 "libraries" 节点的值
            JsonElement.ObjectEnumerator librariesContent = depsJsonRoot.GetProperty("libraries").EnumerateObject();

            var dependencyLibraryList = new List<DependencyLibrary>();

            // 处理 "libraries" 节点的值
            foreach (JsonProperty library in librariesContent)
            {
                // "Azure.Core/1.25.0"
                string libraryName = library.Name;
                string[] libraryNameArr = libraryName.Split("/");

                // 根据 Key，获取 Name 和 Version
                string name = libraryNameArr.Length >= 1 ? libraryNameArr[0] : null;
                string version = libraryNameArr.Length >= 2 ? libraryNameArr[1] : null;

                string type = null;
                if (library.Value.TryGetProperty("type", out JsonElement typeObj))
                {
                    type = typeObj.ToString();
                }

                bool serviceable = false;
                if (library.Value.TryGetProperty("serviceable", out JsonElement serviceableObj))
                {
                    serviceable = serviceableObj.GetBoolean();
                }

                string fileName = targetsMap.GetValueOrDefault(library.Name, name);

                // 放入集合中
                dependencyLibraryList.Add(new DependencyLibrary(type, name, version, fileName, serviceable));
            }

            return dependencyLibraryList;
        }

        return [];
    }

    /// <summary>
    /// 解析当前宿主实际使用的依赖上下文文件
    /// </summary>
    /// <param name="assembly">目标程序集</param>
    /// <returns>解析后的当前宿主实际使用的依赖上下文文件</returns>
    private static string ResolveDependencyContextPath(Assembly assembly)
    {
        if (!string.IsNullOrWhiteSpace(assembly?.Location))
        {
            string assemblyDepsFile = Path.ChangeExtension(assembly.Location, ".deps.json");
            if (File.Exists(assemblyDepsFile))
            {
                return assemblyDepsFile;
            }
        }

        // 测试宿主和插件宿主的入口程序集可能位于 SDK 目录，实际应用的 deps 文件由宿主上下文提供
        string contextDepsFiles = AppContext.GetData("APP_CONTEXT_DEPS_FILES") as string;
        if (!string.IsNullOrWhiteSpace(contextDepsFiles))
        {
            string baseDirectory = Path.GetFullPath(AppContext.BaseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string applicationDepsFile = contextDepsFiles
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(File.Exists)
                .FirstOrDefault(file =>
                    string.Equals(
                        Path.GetDirectoryName(Path.GetFullPath(file))
                            ?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), baseDirectory,
                        StringComparison.OrdinalIgnoreCase));
            if (applicationDepsFile != null)
            {
                return applicationDepsFile;
            }
        }

        return Directory.Exists(AppContext.BaseDirectory)
            ? Directory.EnumerateFiles(AppContext.BaseDirectory, "*.deps.json", SearchOption.TopDirectoryOnly)
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault()
            : null;
    }

    /// <summary>
    /// 获取入口引用程序集
    /// </summary>
    /// <remarks>暂不支持独立/单文件发布</remarks>
    /// <param name="assembly">目标程序集</param>
    /// <param name="dependencyLibraryList">应用依赖库集合</param>
    /// <returns>获取到的入口引用程序集集合</returns>
    public static List<Assembly> GetEntryReferencedAssembly(this Assembly assembly,
        List<DependencyLibrary> dependencyLibraryList = null)
    {
        if (assembly == null)
        {
            return [];
        }

        dependencyLibraryList ??= assembly.GetEntryRuntimeLibraries();

        if (!dependencyLibraryList.Any())
        {
            return [assembly];
        }

        // 已经加载的程序集
        Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

        // 需排除的程序集后缀
        // 这里的 Microsoft.Data.SqlClient 排除是为了解决这个错误 https://github.com/dotnet/SqlClient/issues/1930
        string[] excludeAssemblyNames = new[] {"Database.Migrations", "Microsoft.Data.SqlClient"};

        // 读取项目引用程序集
        return dependencyLibraryList
            .Where(wh => (wh.Type == "project" && !excludeAssemblyNames.Any(a => wh.Name.EndsWith(a))) || wh.Type == "package")
            .Select(sl =>
            {
                // .deps.json 同时包含应用依赖和仅供运行时使用的库；不可加载的条目会被跳过
                Assembly loadedAssembly = loadedAssemblies.FirstOrDefault(f => f
                                                                                   .GetName()
                                                                                   ?.Name?.Equals(sl.FileName,
                                                                                       StringComparison.OrdinalIgnoreCase)
                                                                               == true);
                if (loadedAssembly != null)
                {
                    return loadedAssembly;
                }

                try
                {
                    return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(sl.Name));
                }
                catch
                {
                    try
                    {
                        return Assembly.Load(sl.FileName);
                    }
                    catch
                    {
                        return null;
                    }
                }
            })
            .Where(wh => wh != null)
            .ToList();
    }

    /// <summary>
    /// 获取程序集中所有类型
    /// </summary>
    /// <remarks>默认仅返回公开声明的类型；可通过筛选器进一步限制结果</remarks>
    /// <param name="assembly">目标程序集</param>
    /// <param name="typeFilter">用于筛选程序集类型的委托</param>
    /// <returns>获取到的程序集中所有类型集合</returns>
    public static IEnumerable<Type> GetAssemblyTypes(this Assembly assembly, Func<Type, bool> typeFilter = null)
    {
        Type[] types = Array.Empty<Type>();

        try
        {
            types = assembly.GetTypes();
        }
        catch
        {
            MAppContext.ConsoleWrite(console =>
            {
                console.BackgroundColor = ConsoleColor.DarkRed;
                console.ForegroundColor = ConsoleColor.Black;
                console.Write("fail");
                console.ResetColor();
                console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                console.BackgroundColor = ConsoleColor.DarkRed;
                console.ForegroundColor = ConsoleColor.Black;
                console.WriteLine($"      Error load `{assembly.FullName}` assembly.");
            });
        }

        return types.Where(wh => wh.IsPublic && (typeFilter == null || typeFilter(wh)));
    }
}
