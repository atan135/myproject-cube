using System.Text;

namespace Cube.Shared.Utils;

/// <summary>
/// 增强版日志工具类
/// 支持日志级别控制、文件输出、格式化等功能
/// </summary>
public static class LogUtils
{
    private static readonly object _lock = new();
    private static string _logFilePath = "";
    private static LogLevel _currentLevel = LogLevel.Info;
    private static bool _enableFileLogging = false;

    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    /// <param name="minLevel">最小日志级别</param>
    /// <param name="logFilePath">日志文件路径（为空则不启用文件日志）</param>
    public static void Initialize(LogLevel minLevel = LogLevel.Info, string logFilePath = "")
    {
        _currentLevel = minLevel;
        
        if (!string.IsNullOrEmpty(logFilePath))
        {
            _logFilePath = logFilePath;
            _enableFileLogging = true;
            
            // 确保日志目录存在
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// 设置日志级别
    /// </summary>
    public static void SetLogLevel(LogLevel level)
    {
        _currentLevel = level;
    }

    /// <summary>
    /// Debug级别日志
    /// </summary>
    public static void Debug(string message, params object[] args)
    {
        Log(LogLevel.Debug, message, null, args);
    }

    /// <summary>
    /// Info级别日志
    /// </summary>
    public static void Info(string message, params object[] args)
    {
        Log(LogLevel.Info, message, null, args);
    }

    /// <summary>
    /// Warning级别日志
    /// </summary>
    public static void Warning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, null, args);
    }

    /// <summary>
    /// Error级别日志
    /// </summary>
    public static void Error(string message, Exception? exception = null, params object[] args)
    {
        Log(LogLevel.Error, message, exception, args);
    }

    /// <summary>
    /// Fatal级别日志
    /// </summary>
    public static void Fatal(string message, Exception? exception = null, params object[] args)
    {
        Log(LogLevel.Fatal, message, exception, args);
    }

    /// <summary>
    /// 核心日志方法
    /// </summary>
    private static void Log(LogLevel level, string message, Exception? exception, object[] args)
    {
        // 检查日志级别
        if (level < _currentLevel)
            return;

        // 格式化消息
        string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
        
        // 构造完整日志行
        string logLine = FormatLogEntry(level, formattedMessage, exception);

        // 输出到控制台
        Console.WriteLine(logLine);

        // 写入文件（如果启用）
        if (_enableFileLogging)
        {
            WriteToFile(logLine);
        }
    }

    /// <summary>
    /// 格式化日志条目
    /// </summary>
    private static string FormatLogEntry(LogLevel level, string message, Exception? exception)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper();
        var threadId = Thread.CurrentThread.ManagedThreadId;

        var sb = new StringBuilder();
        sb.Append($"[{timestamp}] [{levelStr}] [T{threadId}] {message}");

        if (exception != null)
        {
            sb.AppendLine();
            sb.Append($"Exception: {exception.GetType().Name}: {exception.Message}");
            sb.AppendLine();
            sb.Append($"Stack Trace: {exception.StackTrace}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 写入日志文件
    /// </summary>
    private static void WriteToFile(string logLine)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // 如果文件写入失败，输出到控制台
                Console.WriteLine($"[ERROR] Failed to write to log file: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取当前日志文件路径
    /// </summary>
    public static string GetLogFilePath()
    {
        return _logFilePath;
    }

    /// <summary>
    /// 检查文件日志是否启用
    /// </summary>
    public static bool IsFileLoggingEnabled()
    {
        return _enableFileLogging;
    }
}