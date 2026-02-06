using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Cube.Framework.UI;

namespace Cube.Game.UI
{
    /// <summary>
    /// UI管理器
    /// 负责管理所有UI界面
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                }
                return _instance;
            }
        }

        // UI缓存字典
        private Dictionary<string, UIBase> _uiCache = new Dictionary<string, UIBase>();
        
        // UI层级容器
        private Dictionary<int, Transform> _layerContainers = new Dictionary<int, Transform>();
        
        // Canvas根节点
        private Transform _canvasRoot;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeCanvas();
        }
        
        /// <summary>
        /// 初始化Canvas层级结构
        /// </summary>
        private void InitializeCanvas()
        {
            _canvasRoot = transform;
            
            // 创建各层级容器
            foreach (Cube.Framework.UI.UILayer layer in System.Enum.GetValues(typeof(Cube.Framework.UI.UILayer)))
            {
                GameObject layerObj = new GameObject(layer.ToString());
                layerObj.layer = LayerMask.NameToLayer("UI");
                Transform layerTransform = layerObj.transform;
                layerTransform.SetParent(_canvasRoot);
                layerTransform.localScale = Vector3.one;
                layerTransform.localPosition = Vector3.zero;
                _layerContainers[(int)layer] = layerTransform;
            }
        }

        /// <summary>
        /// 显示UI界面
        /// </summary>
        public T ShowUI<T>(string uiName) where T : UIBase
        {
            // 从缓存获取或创建UI
            if (!_uiCache.TryGetValue(uiName, out UIBase ui))
            {
                ui = CreateUI<T>(uiName);
                if (ui == null) return null;
                _uiCache[uiName] = ui;
            }
            
            // 设置层级
            SetUILayer(ui, ui.UILayer);
            
            // 显示UI
            ui.Show();
            
            return ui as T;
        }
        
        /// <summary>
        /// 创建UI实例
        /// </summary>
        private T CreateUI<T>(string uiName) where T : UIBase
        {
            // 创建UI对象
            GameObject uiObj = new GameObject(uiName);
            uiObj.layer = LayerMask.NameToLayer("UI");
            
            // 添加必要的组件
            uiObj.AddComponent<RectTransform>();
            var uiDocument = uiObj.AddComponent<UIDocument>();
            
            // 分配PanelSettings
            AssignPanelSettings(uiDocument);
            
            // 加载UXML布局
            LoadUXMLLayout(uiDocument, uiName);
            
            // 添加具体UI脚本
            T uiComponent = uiObj.AddComponent<T>();
            
            return uiComponent;
        }
        
        /// <summary>
        /// 加载UXML布局文件
        /// </summary>
        private void LoadUXMLLayout(UIDocument uiDocument, string uiName)
        {
            if (uiDocument == null) return;
            
            UnityEngine.UIElements.VisualTreeAsset uxmlAsset = null;
            string actualPath = "";
            
            #if UNITY_EDITOR
            // 编辑器环境下尝试多种路径
            string[] editorPaths = {
                "Assets/UI/UXML/LoginPanel.uxml",  // 你的实际文件路径
                $"Assets/UI/UXML/{uiName}.uxml",
                $"Assets/UI/UXML/{uiName}/{uiName}.uxml"  // 备用路径结构
            };
            
            foreach (string path in editorPaths)
            {
                uxmlAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(path);
                if (uxmlAsset != null)
                {
                    actualPath = path;
                    Debug.Log($"Found UXML file at: {path}");
                    break;
                }
                else
                {
                    Debug.Log($"Tried but not found UXML: {path}");
                }
            }
            #else
            // 运行时环境下从Resources加载
            string[] runtimePaths = {
                "UI/UXML/LoginPanel",  // 你的实际文件名
                uiName,
                $"UI/UXML/{uiName}"
            };
            
            foreach (string path in runtimePaths)
            {
                uxmlAsset = Resources.Load<UnityEngine.UIElements.VisualTreeAsset>(path);
                if (uxmlAsset != null)
                {
                    actualPath = path;
                    Debug.Log($"Found UXML in Resources at: {path}");
                    break;
                }
            }
            #endif
            
            if (uxmlAsset != null)
            {
                uiDocument.visualTreeAsset = uxmlAsset;
                Debug.Log($"UXML layout successfully loaded for {uiName} from {actualPath}");
                
                // 加载对应的USS样式文件
                LoadUSSStyles(uiDocument, uiName);
            }
            else
            {
                Debug.LogWarning($"Failed to load UXML layout for {uiName}. Creating basic layout instead.");
                Debug.LogWarning("Please check if your UXML file exists in Assets/UI/UXML/ directory");
                // 创建基本的UI结构作为后备
                CreateBasicUILayout(uiDocument, uiName);
            }
        }
        
        /// <summary>
        /// 加载USS样式文件
        /// </summary>
        private void LoadUSSStyles(UIDocument uiDocument, string uiName)
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;
            
            // 根据实际文件结构定义路径
            string[] ussPaths = {
                "UI/USS/LoginStyle",  // 你的实际文件路径
                "LoginStyle",         // Resources中的相对路径
                $"UI/USS/{uiName}Style",
                $"UI/USS/{uiName}",
                $"{uiName}Style",
                $"{uiName}"
            };
            
            UnityEngine.UIElements.StyleSheet ussAsset = null;
            string actualPath = "";
            
            #if UNITY_EDITOR
            // 编辑器环境下尝试多种路径
            string[] editorPaths = {
                "Assets/UI/USS/LoginStyle.uss",  // 你的实际文件路径
                $"Assets/UI/USS/{uiName}Style.uss",
                $"Assets/UI/USS/{uiName}.uss",
                "Assets/UI/USS/LoginPanel.uss"   // 备用命名
            };
            
            foreach (string path in editorPaths)
            {
                ussAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.StyleSheet>(path);
                if (ussAsset != null)
                {
                    actualPath = path;
                    Debug.Log($"Found USS file at: {path}");
                    break;
                }
                else
                {
                    Debug.Log($"Tried but not found: {path}");
                }
            }
            #else
            // 运行时环境下从Resources加载
            foreach (string path in ussPaths)
            {
                ussAsset = Resources.Load<UnityEngine.UIElements.StyleSheet>(path);
                if (ussAsset != null)
                {
                    actualPath = path;
                    Debug.Log($"Found USS in Resources at: {path}");
                    break;
                }
            }
            #endif
            
            if (ussAsset != null)
            {
                uiDocument.rootVisualElement.styleSheets.Add(ussAsset);
                Debug.Log($"USS styles successfully loaded for {uiName} from {actualPath}");
            }
            else
            {
                Debug.LogWarning($"Failed to load USS styles for {uiName}. Searched in multiple locations.");
                #if UNITY_EDITOR
                Debug.LogWarning("Please check if your USS file exists in Assets/UI/USS/ directory");
                #endif
            }
        }
        
        /// <summary>
        /// 创建基本UI布局（后备方案）
        /// </summary>
        private void CreateBasicUILayout(UIDocument uiDocument, string uiName)
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;
            
            var root = uiDocument.rootVisualElement;
            root.style.flexDirection = UnityEngine.UIElements.FlexDirection.Column;
            root.style.justifyContent = UnityEngine.UIElements.Justify.Center;
            root.style.alignItems = UnityEngine.UIElements.Align.Center;
            root.style.width = new UnityEngine.UIElements.StyleLength(UnityEngine.UIElements.Length.Percent(100));
            root.style.height = new UnityEngine.UIElements.StyleLength(UnityEngine.UIElements.Length.Percent(100));
            
            // 根据UI类型创建相应元素
            if (uiName == "LoginPanel")
            {
                CreateLoginPanelLayout(root);
            }
        }
        
        /// <summary>
        /// 创建登录面板布局
        /// </summary>
        private void CreateLoginPanelLayout(UnityEngine.UIElements.VisualElement root)
        {
            // 主容器
            var container = new UnityEngine.UIElements.VisualElement();
            container.style.backgroundColor = new UnityEngine.UIElements.StyleColor(UnityEngine.Color.white);
            container.style.width = new UnityEngine.UIElements.StyleLength(400);
            container.style.paddingTop = new UnityEngine.UIElements.StyleLength(30);
            container.style.paddingBottom = new UnityEngine.UIElements.StyleLength(30);
            container.style.paddingLeft = new UnityEngine.UIElements.StyleLength(20);
            container.style.paddingRight = new UnityEngine.UIElements.StyleLength(20);
            // 使用分离的圆角属性，确保所有Unity版本兼容
            container.style.borderTopLeftRadius = new UnityEngine.UIElements.StyleLength(10);
            container.style.borderTopRightRadius = new UnityEngine.UIElements.StyleLength(10);
            container.style.borderBottomLeftRadius = new UnityEngine.UIElements.StyleLength(10);
            container.style.borderBottomRightRadius = new UnityEngine.UIElements.StyleLength(10);
            
            // 标题
            var titleLabel = new UnityEngine.UIElements.Label("用户登录");
            titleLabel.style.fontSize = new UnityEngine.UIElements.StyleLength(24);
            titleLabel.style.marginBottom = new UnityEngine.UIElements.StyleLength(20);
            titleLabel.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            container.Add(titleLabel);
            
            // 用户名输入框
            var usernameField = new UnityEngine.UIElements.TextField("用户名");
            usernameField.name = "UsernameInput";
            usernameField.style.marginBottom = new UnityEngine.UIElements.StyleLength(15);
            container.Add(usernameField);
            
            // 密码输入框
            var passwordField = new UnityEngine.UIElements.TextField("密码");
            passwordField.name = "PasswordInput";
            passwordField.style.marginBottom = new UnityEngine.UIElements.StyleLength(15);
            container.Add(passwordField);
            
            // 登录按钮
            var loginButton = new UnityEngine.UIElements.Button();
            loginButton.name = "LoginButton";
            loginButton.text = "登录";
            loginButton.style.height = new UnityEngine.UIElements.StyleLength(40);
            loginButton.style.marginTop = new UnityEngine.UIElements.StyleLength(10);
            container.Add(loginButton);
            
            root.Add(container);
            
            Debug.Log("Basic login panel layout created");
        }
        
        /// <summary>
        /// 为UIDocument分配PanelSettings
        /// </summary>
        private void AssignPanelSettings(UIDocument uiDocument)
        {
            // 使用PanelSettingsManager来处理PanelSettings分配
            PanelSettingsManager.Instance.AssignPanelSettings(uiDocument);
        }
        
        /// <summary>
        /// 设置UI层级
        /// </summary>
        private void SetUILayer(UIBase ui, int layer)
        {
            if (_layerContainers.TryGetValue(layer, out Transform container))
            {
                ui.transform.SetParent(container);
                ui.transform.localScale = Vector3.one;
                ui.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// 隐藏UI界面
        /// </summary>
        public void HideUI(string uiName)
        {
            if (_uiCache.TryGetValue(uiName, out UIBase ui))
            {
                ui.Hide();
            }
        }
        
        /// <summary>
        /// 获取UI实例
        /// </summary>
        public T GetUI<T>(string uiName) where T : UIBase
        {
            if (_uiCache.TryGetValue(uiName, out UIBase ui))
            {
                return ui as T;
            }
            return null;
        }
        
        /// <summary>
        /// 销毁UI
        /// </summary>
        public void DestroyUI(string uiName)
        {
            if (_uiCache.TryGetValue(uiName, out UIBase ui))
            {
                _uiCache.Remove(uiName);
                Destroy(ui.gameObject);
            }
        }
    }
}
