using System.Collections.Generic;

public static class ConfigExtensions
{
    // 解析 Dict 字符串为 Dictionary
    public static Dictionary<string, int> ParseDict(this string rawData)
    {
        var dict = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(rawData)) return dict;

        var pairs = rawData.Split('|');
        foreach (var pair in pairs)
        {
            var kv = pair.Split(':');
            if (kv.Length == 2) dict[kv[0]] = int.Parse(kv[1]);
        }
        return dict;
    }

    // 解析 Array 字符串
    public static string[] ParseArray(this string rawData)
    {
        return string.IsNullOrEmpty(rawData) ? new string[0] : rawData.Split('|');
    }
}