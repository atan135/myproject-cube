using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.IO;
using Cube.Game;
using Cube.Game.UI;
using Cube.Framework.UI;

namespace Cube.Editor
{
    /// <summary>
    /// 场景设置工具
    /// 用于快速设置MainGame场景
    /// </summary>
    public class SceneSetupTool : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("Cube/Setup MainGame Scene")]
        public static void SetupMainGameScene()
        {
            SetupSceneHierarchy();
            SetupMainLoader();
            SetupUIManager();
            Debug.Log("MainGame scene setup completed!");
        }
        
        /// <summary>
        /// 设置场景层级结构
        /// </summary>
        private static void SetupSceneHierarchy()
        {
            // 清理现有对象
            CleanupExistingObjects();
            
            // 创建主要系统对象
            CreateMainSystems();
            
            // 创建UI层级结构
            CreateUIStructure();
            
            Debug.Log("Scene hierarchy setup completed");
        }
        
        /// <summary>
        /// 清理现有对象
        /// </summary>
        private static void CleanupExistingObjects()
        {
            // 删除重复的管理器
            var existingManagers = FindObjectsOfType<MonoBehaviour>();
            foreach (var manager in existingManagers)
            {
                if (manager.GetType().Name.Contains("Manager") && 
                    manager.gameObject.name != "Main Systems")
                {
                    DestroyImmediate(manager.gameObject);
                }
            }
        }
        
        /// <summary>
        /// 创建主要系统对象
        /// </summary>
        private static void CreateMainSystems()
        {
            // 创建主系统容器
            GameObject mainSystems = new GameObject("Main Systems");
            
            // 添加MainLoader
            mainSystems.AddComponent<MainLoader>();
            
            // 确保只有一个相机
            Camera[] cameras = FindObjectsOfType<Camera>();
            if (cameras.Length > 1)
            {
                for (int i = 1; i < cameras.Length; i++)
                {
                    DestroyImmediate(cameras[i].gameObject);
                }
            }
        }
        
        /// <summary>
        /// 创建UI层级结构
        /// </summary>
        private static void CreateUIStructure()
        {
            // 创建UI根对象
            GameObject uiRoot = new GameObject("UI Root");
            uiRoot.layer = LayerMask.NameToLayer("UI");
            
            // 添加Canvas组件
            var canvas = uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            // 添加CanvasScaler
            var scaler = uiRoot.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // 添加GraphicRaycaster
            uiRoot.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // 添加UIManager组件
            uiRoot.AddComponent<UIManager>();
            
            // 创建EventSystem（如果不存在）
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        
        /// <summary>
        /// 设置MainLoader
        /// </summary>
        private static void SetupMainLoader()
        {
            var mainLoader = FindObjectOfType<MainLoader>();
            if (mainLoader == null)
            {
                GameObject mainObj = new GameObject("MainLoader");
                mainLoader = mainObj.AddComponent<MainLoader>();
            }
            
            // 配置MainLoader参数
            // 这些可以在Inspector中手动调整
        }
        
        /// <summary>
        /// 设置UIManager
        /// </summary>
        private static void SetupUIManager()
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("UIManager not found in scene!");
                return;
            }
            
            Debug.Log("UIManager configured successfully");
        }
        
        [MenuItem("Cube/Create Login Panel Prefab")]
        public static void CreateLoginPanelPrefab()
        {
            // 确保PanelSettings存在
            EnsurePanelSettingsExists();
            
            // 创建登录面板预制体
            GameObject loginPanelObj = new GameObject("LoginPanel");
            loginPanelObj.layer = LayerMask.NameToLayer("UI");
            
            // 添加必要组件
            var rectTransform = loginPanelObj.AddComponent<RectTransform>();
            var uiDocument = loginPanelObj.AddComponent<UIDocument>();
            var loginPanel = loginPanelObj.AddComponent<LoginPanel>();
            
            // 分配PanelSettings
            string settingsPath = "Assets/UI/Settings/DefaultPanelSettings.asset";
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            if (panelSettings != null)
            {
                uiDocument.panelSettings = panelSettings;
            }
            
            // 加载UXML模板
            string[] uxmlPaths = {
                "Assets/UI/UXML/LoginPanel.uxml",
                "Assets/UI/UXML/LoginPanel/LoginPanel.uxml"
            };
            
            VisualTreeAsset uxmlAsset = null;
            foreach (string path in uxmlPaths)
            {
                uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (uxmlAsset != null)
                {
                    Debug.Log($"Found UXML at: {path}");
                    break;
                }
            }
            
            if (uxmlAsset != null)
            {
                uiDocument.visualTreeAsset = uxmlAsset;
            }
            
            // 加载USS样式
            string[] ussPaths = {
                "Assets/UI/USS/LoginStyle.uss",
                "Assets/UI/USS/LoginPanelStyle.uss",
                "Assets/UI/USS/LoginPanel/LoginStyle.uss"
            };
            
            StyleSheet ussAsset = null;
            foreach (string path in ussPaths)
            {
                ussAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (ussAsset != null)
                {
                    Debug.Log($"Found USS at: {path}");
                    break;
                }
            }
            
            if (ussAsset != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.styleSheets.Add(ussAsset);
            }
            
            // 设置RectTransform
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // 保存为预制体
            string prefabPath = "Assets/Prefabs/UI/LoginPanel.prefab";
            
            // 确保预制体目录存在
            string prefabDir = Path.GetDirectoryName(prefabPath);
            if (!Directory.Exists(prefabDir))
            {
                Directory.CreateDirectory(prefabDir);
            }
            
            PrefabUtility.SaveAsPrefabAsset(loginPanelObj, prefabPath);
            
            // 清理临时对象
            DestroyImmediate(loginPanelObj);
            
            Debug.Log($"LoginPanel prefab created at: {prefabPath}");
        }
        
        /// <summary>
        /// 确保PanelSettings资源存在
        /// </summary>
        private static void EnsurePanelSettingsExists()
        {
            string settingsPath = "Assets/UI/Settings/DefaultPanelSettings.asset";
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(settingsPath);
            
            if (panelSettings == null)
            {
                // 创建PanelSettings目录
                string settingsDir = Path.GetDirectoryName(settingsPath);
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }
                
                // 创建PanelSettings
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                
                // 配置PanelSettings
                panelSettings.referenceResolution = new Vector2Int(1920, 1080);
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
                panelSettings.sortingOrder = 0;
                
                // 保存资源
                AssetDatabase.CreateAsset(panelSettings, settingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"Created PanelSettings at: {settingsPath}");
            }
        }
        
        [MenuItem("Cube/Test Login Flow")]
        public static void TestLoginFlow()
        {
            var mainLoader = FindObjectOfType<MainLoader>();
            if (mainLoader != null)
            {
                Debug.Log("Testing login flow...");
                // 可以在这里添加测试逻辑
            }
            else
            {
                Debug.LogError("MainLoader not found in scene!");
            }
        }
#endif
    }
}