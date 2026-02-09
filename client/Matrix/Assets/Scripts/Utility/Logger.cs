using UnityEngine;
using System.Diagnostics;

public static class Logger
{
    // 日志总开关，建议在正式发布时设为 false
    public static bool IsEnable = true;

    // 是否显示颜色（某些环境不支持颜色标签可关闭）
    public static bool UseColor = true;

    #region 基础打印

    public static void Info(object msg)
    {
        if (!IsEnable) return;
        UnityEngine.Debug.Log(msg);
    }

    public static void Warning(object msg)
    {
        if (!IsEnable) return;
        UnityEngine.Debug.LogWarning(msg);
    }

    public static void Error(object msg)
    {
        if (!IsEnable) return;
        UnityEngine.Debug.LogError(msg);
    }

    #endregion

    #region 带颜色的模块打印 (推荐)

    public static void Http(object msg) => ColorLog("Cyan", "HTTP", msg);
    public static void Yoo(object msg) => ColorLog("Yellow", "YooAsset", msg);
    public static void Battle(object msg) => ColorLog("Orange", "Battle", msg);
    public static void UI(object msg) => ColorLog("Lime", "UI", msg);

    #endregion

    private static void ColorLog(string color, string tag, object msg)
    {
        if (!IsEnable) return;

        if (UseColor)
            UnityEngine.Debug.Log($"<b>[<color={color}>{tag}</color>]</b> {msg}");
        else
            UnityEngine.Debug.Log($"[{tag}] {msg}");
    }

    /// <summary>
    /// 仅在编辑器环境下生效的调试日志 (不会打进真机包)
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public static void Editor(object msg)
    {
        UnityEngine.Debug.Log($"<color=grey>[EditorOnly]</color> {msg}");
    }
}
