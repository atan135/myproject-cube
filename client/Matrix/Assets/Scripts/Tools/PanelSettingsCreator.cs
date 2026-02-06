using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System.IO;

namespace Cube.Editor
{
    /// <summary>
    /// PanelSettings创建和管理工具
    /// </summary>
    public class PanelSettingsCreator : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Cube/Create Panel Settings")]
        public static void CreatePanelSettings()
        {
            // 创建PanelSettings资源
            string settingsPath = "Assets/UI/Settings/DefaultPanelSettings.asset";
            
            // 检查是否已存在
            var existingSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            if (existingSettings != null)
            {
                Debug.Log($"PanelSettings already exists at: {settingsPath}");
                return;
            }
            
            // 创建新的PanelSettings
            PanelSettings panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            
            // 配置基本设置
            ConfigurePanelSettings(panelSettings);
            
            // 确保目录存在
            string directory = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 保存资源
            AssetDatabase.CreateAsset(panelSettings, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"PanelSettings created at: {settingsPath}");
        }
        
        /// <summary>
        /// 配置PanelSettings参数
        /// </summary>
        private static void ConfigurePanelSettings(PanelSettings panelSettings)
        {
            // 设置参考分辨率
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            
            // 设置缩放模式
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            
            // 设置匹配模式
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f; // 0=匹配宽度, 1=匹配高度, 0.5=平衡
            
            // 设置剔除掩码
            // panelSettings.pickingMode = PickingMode.Position; // 此属性在某些Unity版本中不可用
            
            // 设置渲染队列
            panelSettings.sortingOrder = 0;
            
            // 启用反锯齿
            // panelSettings.antialiasing = 1; // 此属性在某些Unity版本中不可用
            
            // 设置透明度支持
            // panelSettings.clearColor = Color.clear; // 此属性在某些Unity版本中不可用
            // panelSettings.colorClearValue = Color.clear; // 此属性在某些Unity版本中不可用
        }
        
        /// <summary>
        /// 自动为场景中的UIDocument分配PanelSettings
        /// </summary>
        [MenuItem("Cube/Assign Panel Settings to UIDocuments")]
        public static void AssignPanelSettingsToDocuments()
        {
            // 查找PanelSettings资源
            string settingsPath = "Assets/UI/Settings/DefaultPanelSettings.asset";
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            
            if (panelSettings == null)
            {
                Debug.LogWarning("PanelSettings not found. Creating new one...");
                CreatePanelSettings();
                panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            }
            
            if (panelSettings == null)
            {
                Debug.LogError("Failed to create or load PanelSettings");
                return;
            }
            
            // 查找场景中所有的UIDocument
            UIDocument[] uiDocuments = FindObjectsOfType<UIDocument>();
            int assignedCount = 0;
            
            foreach (UIDocument uiDoc in uiDocuments)
            {
                if (uiDoc.panelSettings == null)
                {
                    uiDoc.panelSettings = panelSettings;
                    assignedCount++;
                    Debug.Log($"Assigned PanelSettings to {uiDoc.name}");
                }
            }
            
            if (assignedCount > 0)
            {
                EditorUtility.SetDirty(panelSettings);
                AssetDatabase.SaveAssets();
                Debug.Log($"Successfully assigned PanelSettings to {assignedCount} UIDocument(s)");
            }
            else
            {
                Debug.Log("All UIDocuments already have PanelSettings assigned");
            }
        }
        
        /// <summary>
        /// 检查并修复场景中的UIDocument设置
        /// </summary>
        [MenuItem("Cube/Fix UIDocument Settings")]
        public static void FixUIDocumentSettings()
        {
            // 确保PanelSettings存在
            string settingsPath = "Assets/UI/Settings/DefaultPanelSettings.asset";
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            
            if (panelSettings == null)
            {
                CreatePanelSettings();
                panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            }
            
            // 查找所有UIDocument
            UIDocument[] uiDocuments = FindObjectsOfType<UIDocument>();
            
            foreach (UIDocument uiDoc in uiDocuments)
            {
                bool needsUpdate = false;
                
                // 检查PanelSettings
                if (uiDoc.panelSettings == null)
                {
                    uiDoc.panelSettings = panelSettings;
                    needsUpdate = true;
                    Debug.Log($"Fixed PanelSettings for {uiDoc.name}");
                }
                
                // 检查Sorting Order
                if (uiDoc.sortingOrder != 0)
                {
                    uiDoc.sortingOrder = 0;
                    needsUpdate = true;
                }
                
                if (needsUpdate)
                {
                    EditorUtility.SetDirty(uiDoc);
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("UIDocument settings fix completed");
        }
#endif
    }
}