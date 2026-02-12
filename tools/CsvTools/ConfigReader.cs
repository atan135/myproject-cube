using System.Text.Json;

namespace CsvTools;

/// <summary>
/// 简洁的配置读取器
/// 使用System.Text.Json读取appsettings.json配置文件
/// </summary>
public static class ConfigReader
{
    private static Dictionary<string, object>? _configCache;
    private static readonly object _lock = new();
    private static bool _initialized = false;

    /// <summary>
    /// 初始化配置读取器
    /// </summary>
    public static void Initialize(string configPath = "appsettings.json")
    {
        lock (_lock)
        {
            if (_initialized) return;

            try
            {
                var fullPath = Path.GetFullPath(configPath);
                Console.WriteLine($"[Config] 正在加载配置文件: {fullPath}");

                if (!File.Exists(fullPath))
                {
                    Console.WriteLine($"[Config] 配置文件不存在: {fullPath}");
                    _configCache = new Dictionary<string, object>();
                    _initialized = true;
                    return;
                }

                var jsonString = File.ReadAllText(fullPath);
                var jsonDoc = JsonDocument.Parse(jsonString);
                
                _configCache = ParseJsonElement(jsonDoc.RootElement);
                _initialized = true;
                
                Console.WriteLine("[Config] 配置文件加载成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] 配置文件加载失败: {ex.Message}");
                _configCache = new Dictionary<string, object>();
                _initialized = true;
            }
        }
    }

    /// <summary>
    /// 获取字符串配置值
    /// </summary>
    public static string GetString(string key, string defaultValue = "")
    {
        EnsureInitialized();
        return GetValue<object>(key)?.ToString() ?? defaultValue;
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
    /// 获取浮点数配置值
    /// </summary>
    public static float GetFloat(string key, float defaultValue = 0f)
    {
        var value = GetString(key);
        return float.TryParse(value, out float result) ? result : defaultValue;
    }

    /// <summary>
    /// 获取必需的配置值（不能为空）
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
    /// 检查配置项是否存在
    /// </summary>
    public static bool Exists(string key)
    {
        EnsureInitialized();
        return GetValue<object>(key) != null;
    }

    /// <summary>
    /// 显示所有配置（隐藏敏感信息）
    /// </summary>
    public static void DisplayConfig()
    {
        EnsureInitialized();
        if (_configCache == null) return;

        Console.WriteLine("=== 当前配置 ===");
        DisplayDictionary(_configCache, "");
        Console.WriteLine("================");
    }

    #region 私有方法

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            Initialize();
        }
    }

    private static T? GetValue<T>(string key)
    {
        if (_configCache == null || string.IsNullOrEmpty(key))
            return default(T);

        var keys = key.Split(':');
        var current = _configCache;

        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (current.TryGetValue(keys[i], out var value) && value is Dictionary<string, object> dict)
            {
                current = dict;
            }
            else
            {
                return default(T);
            }
        }

        if (current.TryGetValue(keys[keys.Length - 1], out var finalValue))
        {
            if (finalValue is JsonElement element)
            {
                return ConvertJsonElement<T>(element);
            }
            return (T?)finalValue;
        }

        return default(T);
    }

    private static Dictionary<string, object> ParseJsonElement(JsonElement element)
    {
        var result = new Dictionary<string, object>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ParseJsonValue(property.Value);
            }
        }

        return result;
    }

    private static object ParseJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out int intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => ParseJsonElement(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ParseJsonValue).ToArray(),
            _ => element.ToString() ?? ""
        };
    }

    private static T? ConvertJsonElement<T>(JsonElement element)
    {
        try
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => (T?)(object)(element.GetString() ?? ""),
                JsonValueKind.Number => typeof(T) == typeof(int) ? (T?)(object)element.GetInt32() :
                                      typeof(T) == typeof(float) ? (T?)(object)element.GetSingle() :
                                      typeof(T) == typeof(double) ? (T?)(object)element.GetDouble() :
                                      (T?)(object)element.GetRawText(),
                JsonValueKind.True => (T?)(object)true,
                JsonValueKind.False => (T?)(object)false,
                _ => default(T)
            };
        }
        catch
        {
            return default(T);
        }
    }

    private static void DisplayDictionary(Dictionary<string, object> dict, string prefix)
    {
        foreach (var kvp in dict)
        {
            var key = $"{prefix}{kvp.Key}";
            var value = kvp.Value;

            if (value is Dictionary<string, object> subDict)
            {
                DisplayDictionary(subDict, $"{key}:");
            }
            else
            {
                // 隐藏敏感信息
                var displayValue = key.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                                 key.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                                 key.Contains("Key", StringComparison.OrdinalIgnoreCase)
                    ? "******"
                    : value.ToString();

                Console.WriteLine($"{key}: {displayValue}");
            }
        }
    }

    #endregion
}