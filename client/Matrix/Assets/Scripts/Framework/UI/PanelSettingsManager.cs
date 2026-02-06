using UnityEngine;
using UnityEngine.UIElements;

namespace Cube.Framework.UI
{
    /// <summary>
    /// 运行时PanelSettings管理器
    /// 确保PanelSettings在运行时能够正确加载和分配
    /// </summary>
    public class PanelSettingsManager : MonoBehaviour
    {
        private static PanelSettingsManager _instance;
        public static PanelSettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PanelSettingsManager>();
                    if (_instance == null)
                    {
                        // 创建新的管理器实例
                        GameObject managerObj = new GameObject("PanelSettingsManager");
                        _instance = managerObj.AddComponent<PanelSettingsManager>();
                        DontDestroyOnLoad(managerObj);
                    }
                }
                return _instance;
            }
        }
        
        private UnityEngine.UIElements.PanelSettings _defaultPanelSettings;
        private bool _isInitialized = false;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        /// <summary>
        /// 初始化PanelSettings管理器
        /// </summary>
        private void Initialize()
        {
            if (_isInitialized) return;
            
            LoadDefaultPanelSettings();
            _isInitialized = true;
        }
        
        /// <summary>
        /// 加载默认PanelSettings
        /// </summary>
        private void LoadDefaultPanelSettings()
        {
            // 尝试多种加载方式
            _defaultPanelSettings = LoadPanelSettingsFromResources();
            
            if (_defaultPanelSettings == null)
            {
                _defaultPanelSettings = CreateDefaultPanelSettings();
            }
            
            if (_defaultPanelSettings != null)
            {
                Debug.Log("Default PanelSettings loaded successfully");
            }
            else
            {
                Debug.LogError("Failed to load or create default PanelSettings");
            }
        }
        
        /// <summary>
        /// 从Resources目录加载PanelSettings
        /// </summary>
        private UnityEngine.UIElements.PanelSettings LoadPanelSettingsFromResources()
        {
            // 尝试从Resources加载
            var panelSettings = Resources.Load<UnityEngine.UIElements.PanelSettings>("UI/Settings/DefaultPanelSettings");
            if (panelSettings != null)
            {
                Debug.Log("PanelSettings loaded from Resources");
                EnsureThemeStyleSheet(panelSettings);
                return panelSettings;
            }
            
            // 尝试其他可能的路径
            panelSettings = Resources.Load<UnityEngine.UIElements.PanelSettings>("DefaultPanelSettings");
            if (panelSettings != null)
            {
                Debug.Log("PanelSettings loaded from Resources (alternative path)");
                EnsureThemeStyleSheet(panelSettings);
                return panelSettings;
            }
            
            return null;
        }
        
        /// <summary>
        /// 确保PanelSettings有有效的Theme StyleSheet
        /// </summary>
        private void EnsureThemeStyleSheet(UnityEngine.UIElements.PanelSettings panelSettings)
        {
            if (panelSettings == null) return;
            
            // 如果没有Theme StyleSheet，尝试分配一个
            if (panelSettings.themeStyleSheet == null)
            {
                AssignDefaultThemeStyleSheet(panelSettings);
            }
        }
        
        /// <summary>
        /// 分配默认主题样式表
        /// </summary>
        private void AssignDefaultThemeStyleSheet(UnityEngine.UIElements.PanelSettings panelSettings)
        {
            if (panelSettings == null) return;
            
            UnityEngine.UIElements.ThemeStyleSheet themeSheet = null;
            
            // 1. 首先尝试加载我们创建的默认主题
            string customThemePath = "Assets/UI/Themes/DefaultTheme.tss";
            #if UNITY_EDITOR
            themeSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(customThemePath);
            #else
            themeSheet = Resources.Load<UnityEngine.UIElements.ThemeStyleSheet>("UI/Themes/DefaultTheme");
            #endif
            
            if (themeSheet != null)
            {
                panelSettings.themeStyleSheet = themeSheet;
                Debug.Log("Custom default theme style sheet assigned");
                return;
            }
            
            // 2. 尝试加载Unity内置主题
            #if UNITY_EDITOR
            string[] builtinPaths = {
                "Packages/com.unity.ui.builder/Editor/Resources/StyleSheets/Default/DefaultTheme.tss",
                "Packages/com.unity.editor.ui/Editor/StyleSheets/Default/DefaultTheme.tss"
            };
            
            foreach (string path in builtinPaths)
            {
                themeSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(path);
                if (themeSheet != null)
                {
                    panelSettings.themeStyleSheet = themeSheet;
                    Debug.Log($"Built-in theme style sheet assigned from: {path}");
                    return;
                }
            }
            #endif
            
            // 3. 如果都失败了，记录警告但继续运行
            Debug.LogWarning("No theme style sheet could be loaded. UI may not render properly.");
        }
        
        /// <summary>
        /// 创建默认PanelSettings（备用方案）
        /// </summary>
        private UnityEngine.UIElements.PanelSettings CreateDefaultPanelSettings()
        {
            Debug.Log("Creating default PanelSettings programmatically");
            
            var panelSettings = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
            
            // 配置基本参数
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
            panelSettings.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = 0;
            
            // 尝试加载默认主题样式表
            AssignDefaultThemeStyleSheet(panelSettings);
            
            return panelSettings;
        }
        
        /// <summary>
        /// 获取默认PanelSettings
        /// </summary>
        public UnityEngine.UIElements.PanelSettings GetDefaultPanelSettings()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            
            return _defaultPanelSettings;
        }
        
        /// <summary>
        /// 为UIDocument分配PanelSettings
        /// </summary>
        public void AssignPanelSettings(UIDocument uiDocument)
        {
            if (uiDocument == null) return;
            
            if (uiDocument.panelSettings == null)
            {
                var panelSettings = GetDefaultPanelSettings();
                if (panelSettings != null)
                {
                    uiDocument.panelSettings = panelSettings;
                    Debug.Log($"PanelSettings assigned to {uiDocument.name}");
                }
                else
                {
                    Debug.LogWarning($"Cannot assign PanelSettings to {uiDocument.name} - no default PanelSettings available");
                }
            }
        }
        
        /// <summary>
        /// 预加载PanelSettings资源
        /// </summary>
        public void PreloadPanelSettings()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
    }
}