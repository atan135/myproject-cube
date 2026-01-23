namespace Cube.Shared.Utils;

/// <summary>
/// 日志工具类
/// </summary>
public static class Logger
{
    public static void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }

    public static void LogError(string message, Exception? ex = null)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
        if (ex != null)
        {
            Console.WriteLine($"Exception: {ex}");
        }
    }

    public static void LogWarning(string message)
    {
        Console.WriteLine($"[WARNING] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}");
    }
}
