// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Globalization;
using System.Text;

namespace Fast.Logging;

/// <summary>
/// 文件日志写入器
/// </summary>
internal sealed class FileLoggingWriter : IDisposable
{
    /// <summary>
    /// 文件日志记录器提供程序
    /// </summary>
    private readonly FileLoggerProvider _fileLoggerProvider;

    /// <summary>
    /// 日志配置选项
    /// </summary>
    private readonly FileLoggerOptions _options;

    /// <summary>
    /// 日志文件名
    /// </summary>
    private string _fileName;

    /// <summary>
    /// 文件流
    /// </summary>
    private FileStream _fileStream;

    /// <summary>
    /// 文本写入器
    /// </summary>
    private StreamWriter _textWriter;

    /// <summary>
    /// 缓存上次返回的基本日志文件名，避免重复解析
    /// </summary>
    private string __LastBaseFileName;

    /// <summary>
    /// 判断是否启动滚动日志功能
    /// </summary>
    private readonly bool _isEnabledRollingFiles;

    /// <summary>
    /// 上次尝试重新打开文件的时间（UTC），用于控制重试冷却
    /// </summary>
    private DateTime _lastReopenAttempt = DateTime.MinValue;

    /// <summary>
    /// 重新打开文件的最小间隔时间（5 秒），避免在持续失败时频繁重试导致性能损耗
    /// </summary>
    private static readonly TimeSpan _reopenInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 初始化类的新实例
    /// </summary>
    /// <param name="fileLoggerProvider">文件日志记录器提供程序</param>
    internal FileLoggingWriter(FileLoggerProvider fileLoggerProvider)
    {
        _fileLoggerProvider = fileLoggerProvider;
        _options = fileLoggerProvider.LoggerOptions;
        _isEnabledRollingFiles = _options.MaxRollingFiles > 0 && _options.FileSizeLimitBytes > 0;

        // 解析当前写入日志的文件名
        GetCurrentFileName();

        // 打开文件并持续写入，如果失败则记录错误但不抛出异常，后续写入时会自动重试
        try
        {
            OpenFile(true);
        }
        catch (Exception ex)
        {
            // 在 Linux 下可能因为权限、路径等问题导致文件创建失败
            // 输出诊断信息到标准错误流，帮助用户排查问题
            Console.Error.WriteLine($"[Fast.Logging] Failed to create log file '{_fileName}': {ex.Message}");
        }
    }

    /// <summary>
    /// 获取日志基础文件名
    /// </summary>
    /// <returns>日志文件名</returns>
    private string GetBaseFileName()
    {
        string fileName = _fileLoggerProvider.FileName;

        // 如果配置了日志文件名格式化程序，则先处理再返回
        if (_options.FileNameRule != null)
        {
            fileName = _options.FileNameRule(fileName);
        }

        return fileName;
    }

    /// <summary>
    /// 解析当前写入日志的文件名
    /// </summary>
    private void GetCurrentFileName()
    {
        // 获取日志基础文件名并将其缓存
        string baseFileName = GetBaseFileName();
        __LastBaseFileName = baseFileName;

        // 是否配置了日志文件最大存储大小
        if (_options.FileSizeLimitBytes > 0)
        {
            // 定义文件查找通配符
            string logFileMask = Path.GetFileNameWithoutExtension(baseFileName) + "*" + Path.GetExtension(baseFileName);

            // 获取文件路径
            string logDirName = Path.GetDirectoryName(baseFileName);

            // 如果没有配置文件路径则默认放置根目录
            if (string.IsNullOrEmpty(logDirName))
            {
                logDirName = Directory.GetCurrentDirectory();
            }

            // 在当前目录下根据文件通配符查找所有匹配的文件
            string[] logFiles = Directory.Exists(logDirName)
                ? Directory.GetFiles(logDirName, logFileMask, SearchOption.TopDirectoryOnly)
                : [];

            // 处理已有日志文件存在情况
            if (logFiles.Length > 0)
            {
                // 根据文件名和最后更新时间获取最近操作的文件
                FileInfo lastFileInfo = logFiles.Select(fName => new FileInfo(fName))
                    .OrderByDescending(fInfo => fInfo.Name)
                    .ThenByDescending(fInfo => fInfo.LastWriteTime)
                    .First();

                _fileName = lastFileInfo.FullName;
            }
            // 没有任何匹配的日志文件直接使用当前基础文件名
            else
            {
                _fileName = baseFileName;
            }
        }
        else
        {
            _fileName = baseFileName;
        }
    }

    /// <summary>
    /// 获取下一个匹配的日志文件名
    /// </summary>
    /// <remarks>只有配置了 <see cref="FileLoggerOptions.FileSizeLimitBytes"/> 或 <see cref="FileLoggerOptions.FileNameRule"/> 或 <see cref="FileLoggerOptions.MaxRollingFiles"/> 有效</remarks>
    /// <returns>新的文件名</returns>
    private string GetNextFileName()
    {
        // 获取日志基础文件名
        string baseFileName = GetBaseFileName();

        // 如果文件不存在或没有达到 FileSizeLimitBytes 限制大小，则返回基础文件名
        if (!File.Exists(baseFileName)
            || _options.FileSizeLimitBytes <= 0
            || new FileInfo(baseFileName).Length < _options.FileSizeLimitBytes)
        {
            return baseFileName;
        }

        // 获取日志基础文件名和当前日志文件名
        int currentFileIndex = 0;
        string baseFileNameOnly = Path.GetFileNameWithoutExtension(baseFileName);
        string currentFileNameOnly = Path.GetFileNameWithoutExtension(_fileName);

        // 解析日志文件名【递增】部分
        string suffix = currentFileNameOnly != null && currentFileNameOnly.StartsWith(baseFileNameOnly, StringComparison.Ordinal)
            ? currentFileNameOnly[baseFileNameOnly.Length..]
            : null;
        if (suffix?.Length > 0 && int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedIndex))
        {
            currentFileIndex = parsedIndex;
        }

        // 【递增】部分 +1
        int nextFileIndex = currentFileIndex + 1;

        // 如果配置了最大【递增】数，则超出自动从头开始（覆盖写入）
        if (_options.MaxRollingFiles > 0)
        {
            nextFileIndex %= _options.MaxRollingFiles;
        }

        // 返回下一个匹配的日志文件名（完整路径）
        string nextFileName = baseFileNameOnly
                              + (nextFileIndex > 0 ? nextFileIndex.ToString(CultureInfo.InvariantCulture) : "")
                              + Path.GetExtension(baseFileName);
        return Path.Combine(Path.GetDirectoryName(baseFileName), nextFileName);
    }

    /// <summary>
    /// 打开文件
    /// </summary>
    /// <param name="append">是否追加写入</param>
    private void OpenFile(bool append)
    {
        try
        {
            CreateFileStream();
        }
        catch (Exception ex)
        {
            // 处理文件写入错误
            if (_options.HandleWriteError != null)
            {
                var fileWriteError = new FileWriteError(_fileName, ex);
                _options.HandleWriteError(fileWriteError);

                // 如果配置了备用文件名，则重新写入
                if (fileWriteError.RollbackFileName != null)
                {
                    _fileLoggerProvider.FileName = fileWriteError.RollbackFileName;

                    // 递归操作，直到应用程序停止
                    GetCurrentFileName();
                    CreateFileStream();
                }
            }
            // 其他直接抛出异常
            else
            {
                throw;
            }
        }

        // 初始化文本写入器（显式指定 UTF-8 编码，跨平台一致）
        _textWriter = new StreamWriter(_fileStream, Encoding.UTF8);

        // 创建文件流
        void CreateFileStream()
        {
            var fileInfo = new FileInfo(_fileName);

            // 判断文件目录是否存在，不存在则自动创建
            fileInfo.Directory?.Create();

            // 创建文件流，允许其他进程读取但不允许写入，避免日志数据竞争
            // 不使用 FileOptions.WriteThrough，在 Linux/macOS 上会映射为 O_SYNC 导致严重性能下降
            _fileStream = new FileStream(_fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096,
                FileOptions.None);

            // 删除超出滚动日志限制的文件
            DropFilesIfOverLimit(fileInfo);

            // 判断是否追加还是覆盖
            if (append)
            {
                _fileStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                _fileStream.SetLength(0);
            }
        }
    }

    /// <summary>
    /// 判断是否需要创建新文件写入
    /// </summary>
    private void CheckForNewLogFile()
    {
        bool openNewFile = IsMaxFileSizeThresholdReached() || IsBaseFileNameChanged() || IsFileDeletedExternally();

        // 重新创建新文件并写入
        if (openNewFile)
        {
            Close();

            // 计算新文件名
            _fileName = GetNextFileName();

            // 打开新文件并写入，如果失败则记录错误（_textWriter 会保持 null，Write 方法后续会通过 TryReopenFile 重试）
            try
            {
                OpenFile(false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Fast.Logging] Failed to create new log file '{_fileName}': {ex.Message}");
            }
        }

        // 是否超出限制的最大大小
        bool IsMaxFileSizeThresholdReached()
        {
            return _options.FileSizeLimitBytes > 0 && _fileStream != null && _fileStream.Length >= _options.FileSizeLimitBytes;
        }

        // 是否重新自定义了文件名
        bool IsBaseFileNameChanged()
        {
            if (_options.FileNameRule != null)
            {
                string baseFileName = GetBaseFileName();

                if (baseFileName != __LastBaseFileName)
                {
                    __LastBaseFileName = baseFileName;
                    return true;
                }
            }

            return false;
        }

        // 日志文件是否被外部进程删除
        // 在 Linux 上，文件被删除后 FileStream 句柄仍然有效（写入到已删除的 inode），但文件在文件系统中不可见
        // 此检查确保当文件被外部进程（如日志清理服务）删除时，能够自动重新创建文件
        bool IsFileDeletedExternally()
        {
            return _fileStream != null && !File.Exists(_fileName);
        }
    }

    /// <summary>
    /// 删除超出滚动日志限制的文件
    /// </summary>
    /// <param name="fileInfo">正在检查保留策略的日志文件</param>
    private void DropFilesIfOverLimit(FileInfo fileInfo)
    {
        // 判断是否启用滚动文件功能
        if (!_isEnabledRollingFiles)
        {
            return;
        }

        // 处理 Windows 和 Linux 路径分隔符不一致问题
        string fName = fileInfo.FullName.Replace('\\', '/');

        // 将当前文件名存储到集合中
        bool succeed = _fileLoggerProvider._rollingFileNames.TryAdd(fName, fileInfo);

        // 判断超出限制的文件自动删除
        if (succeed && _fileLoggerProvider._rollingFileNames.Count > _options.MaxRollingFiles)
        {
            // 根据最后写入时间删除过时日志
            IEnumerable<KeyValuePair<string, FileInfo>> dropFiles = _fileLoggerProvider._rollingFileNames
                .OrderBy(u => u.Value.LastWriteTimeUtc)
                .Take(_fileLoggerProvider._rollingFileNames.Count - _options.MaxRollingFiles);

            // 遍历所有需要删除的文件
            foreach (KeyValuePair<string, FileInfo> rollingFile in dropFiles)
            {
                bool removeSucceed = _fileLoggerProvider._rollingFileNames.TryRemove(rollingFile.Key, out _);
                if (!removeSucceed)
                {
                    continue;
                }

                // 当前方法本来就在专用日志线程执行，无需再创建无法观察异常的后台任务
                try
                {
                    if (File.Exists(rollingFile.Key))
                    {
                        File.Delete(rollingFile.Key);
                    }
                }
                catch (IOException)
                {
                    _fileLoggerProvider._rollingFileNames.TryAdd(rollingFile.Key, rollingFile.Value);
                }
                catch (UnauthorizedAccessException)
                {
                    _fileLoggerProvider._rollingFileNames.TryAdd(rollingFile.Key, rollingFile.Value);
                }
            }
        }
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="logMsg">日志消息</param>
    /// <param name="flush">写入后是否立即刷新缓冲区</param>
    internal void Write(LogMessage logMsg, bool flush)
    {
        // 如果文本写入器为空，尝试重新打开文件（支持从构造函数失败或文件轮转失败中恢复）
        if (_textWriter == null)
        {
            TryReopenFile();
            if (_textWriter == null)
            {
                return;
            }
        }

        CheckForNewLogFile();

        // CheckForNewLogFile 内部 Close() 后若 OpenFile() 失败，_textWriter 可能为 null
        if (_textWriter == null)
        {
            return;
        }

        _textWriter.WriteLine(logMsg.Message);

        if (flush)
        {
            _textWriter.Flush();
        }
    }

    /// <summary>
    /// 尝试重新打开日志文件（带有冷却时间以避免频繁重试）
    /// </summary>
    private void TryReopenFile()
    {
        // 限制重试频率，避免因持续失败导致性能问题
        if (DateTime.UtcNow - _lastReopenAttempt < _reopenInterval)
        {
            return;
        }

        _lastReopenAttempt = DateTime.UtcNow;

        try
        {
            GetCurrentFileName();
            OpenFile(true);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Fast.Logging] Failed to reopen log file '{_fileName}': {ex.Message}");
        }
    }

    /// <summary>
    /// 关闭文本写入器并释放
    /// </summary>
    internal void Close()
    {
        if (_textWriter == null && _fileStream == null)
        {
            return;
        }

        StreamWriter textWriter = _textWriter;
        _textWriter = null;

        FileStream fileStream = _fileStream;
        _fileStream = null;

        textWriter?.Dispose();
        fileStream?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }
}
