using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Cube.Shared.Utils;

/// <summary>
/// 简化版全局配置管理器
/// 类似Node.js的dotenv，易于使用
/// </summary>
public static class SimpleConfig
{
    private static IConfiguration? _configuration;
    private static readonly object _lock = new();

    /// <summary>
    /// 初始化配置系统
    /// </summary>
    public static void Initialize(string environment = "Development")
    {
        lock (_lock)
        {
            if (_configuration != null)
                return;

            var builder = new ConfigurationBuilder()
                .SetBasePath(GetBasePath())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            _configuration = builder.Build();
        }
    }

    /// <summary>
    /// 从.env文件加载环境变量
    /// </summary>
    public static void LoadEnv(string envFilePath = ".env")
    {
        var envFile = Path.Combine(GetBasePath(), envFilePath);
        
        if (File.Exists(envFile))
        {
            foreach (var line in File.ReadAllLines(envFile))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim().Trim('"', '\'');
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }

    /// <summary>
    /// 获取字符串配置值
    /// </summary>
    public static string? GetString(string key, string? defaultValue = null)
    {
        EnsureInitialized();
        return _configuration?[key] ?? defaultValue ?? Environment.GetEnvironmentVariable(key);
    }

    /// <summary>
    /// 获取整数配置值
    /// </summary>
    public static int GetInt(string key, int defaultValue = 0)
    {
        var value = GetString(key);
        return int.TryParse(value, out int result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取布尔配置值
    /// </summary>
    public static bool GetBool(string key, bool defaultValue = false)
    {
        var value = GetString(key);
        return bool.TryParse(value, out bool result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取必须的配置值
    /// </summary>
    public static string GetRequiredString(string key)
    {
        var value = GetString(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"配置项 '{key}' 是必需的但未找到");
        }
        return value;
    }

    /// <summary>
    /// 获取连接字符串
    /// </summary>
    public static string? GetConnectionString(string name)
    {
        EnsureInitialized();
        return _configuration?.GetConnectionString(name);
    }

    /// <summary>
    /// 获取所有配置（用于调试）
    /// </summary>
    public static Dictionary<string, string?> GetAll()
    {
        EnsureInitialized();
        var result = new Dictionary<string, string?>();
        
        if (_configuration != null)
        {
            foreach (var provider in ((ConfigurationRoot)_configuration).Providers)
            {
                ExtractKeys(provider, "", result);
            }
        }
        
        return result;
    }

    private static void ExtractKeys(IConfigurationProvider provider, string path, Dictionary<string, string?> result)
    {
        provider.GetChildKeys(Array.Empty<string>(), path)
            .ToList()
            .ForEach(key =>
            {
                var fullPath = string.IsNullOrEmpty(path) ? key : $"{path}:{key}";
                if (provider.TryGet(fullPath, out var value))
                {
                    result[fullPath] = value;
                }
                ExtractKeys(provider, fullPath, result);
            });
    }

    private static void EnsureInitialized()
    {
        if (_configuration == null)
        {
            lock (_lock)
            {
                if (_configuration == null)
                {
                    Initialize();
                }
            }
        }
    }

    private static string GetBasePath()
    {
        // 对于Web应用，使用ContentRootPath
        var contentRoot = Environment.GetEnvironmentVariable("ASPNETCORE_CONTENTROOT");
        if (!string.IsNullOrEmpty(contentRoot))
        {
            return contentRoot;
        }
        
        // 对于控制台应用，使用当前目录
        return Directory.GetCurrentDirectory();
    }
}