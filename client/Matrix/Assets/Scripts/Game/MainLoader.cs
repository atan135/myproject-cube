using UnityEngine;
using Cube.Framework.UI;
using Cube.Game.UI;
using Cube.Network;
using System.Collections;

namespace Cube.Game
{
    /// <summary>
    /// 主启动器
    /// 负责游戏启动流程控制
    /// </summary>
    public class MainLoader : MonoBehaviour
    {
        [Header("启动配置")]
        [SerializeField] private bool showLoginOnStart = true;
        [SerializeField] private float splashScreenDuration = 2.0f;
        [SerializeField] private bool autoConnectServer = true;
        
        [Header("调试选项")]
        [SerializeField] private bool skipSplashScreen = false;
        [SerializeField] private bool autoLoginWithSavedCredentials = false;
        
        // 单例实例
        private static MainLoader _instance;
        public static MainLoader Instance => _instance;
        
        // 启动阶段枚举
        public enum LaunchPhase
        {
            Initializing,
            SplashScreen,
            MainMenu,
            Login,
            GameLoading,
            InGame
        }
        
        private LaunchPhase _currentPhase = LaunchPhase.Initializing;
        public LaunchPhase CurrentPhase => _currentPhase;
        
        private void Awake()
        {
            // 确保单例
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 设置目标帧率
            Application.targetFrameRate = 60;
            
            Debug.Log("MainLoader initialized");
        }
        
        private void Start()
        {
            StartCoroutine(LaunchSequence());
        }
        
        /// <summary>
        /// 启动序列
        /// </summary>
        private IEnumerator LaunchSequence()
        {
            Debug.Log("Starting launch sequence...");
            
            // 阶段1: 初始化
            yield return InitializeSystems();
            
            // 阶段2: 启动画面（可选）
            if (!skipSplashScreen)
            {
                yield return ShowSplashScreen();
            }
            
            // 阶段3: 显示主菜单或直接进入登录
            if (showLoginOnStart)
            {
                yield return ShowLoginInterface();
            }
            else
            {
                yield return ShowMainMenu();
            }
            
            Debug.Log("Launch sequence completed");
        }
        
        /// <summary>
        /// 初始化系统
        /// </summary>
        private IEnumerator InitializeSystems()
        {
            _currentPhase = LaunchPhase.Initializing;
            Debug.Log("Initializing game systems...");
            
            // 初始化网络管理器
            InitializeNetworkManager();
            
            // 初始化UI管理器
            InitializeUIManager();
            
            // 初始化其他必要系统
            InitializeOtherSystems();
            
            // 等待一帧确保所有初始化完成
            yield return null;
            
            Debug.Log("Systems initialized successfully");
        }
        
        /// <summary>
        /// 初始化网络管理器
        /// </summary>
        private void InitializeNetworkManager()
        {
            if (NetworkManager.Instance == null)
            {
                GameObject networkObj = new GameObject("NetworkManager");
                networkObj.AddComponent<NetworkManager>();
                DontDestroyOnLoad(networkObj);
            }
            
            Debug.Log("NetworkManager initialized");
        }
        
        /// <summary>
        /// 初始化UI管理器
        /// </summary>
        private void InitializeUIManager()
        {
            if (UIManager.Instance == null)
            {
                GameObject uiObj = new GameObject("UIManager");
                uiObj.AddComponent<UIManager>();
                DontDestroyOnLoad(uiObj);
                
                // 添加Canvas组件
                var canvas = uiObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // 添加CanvasScaler
                var scaler = uiObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // 添加GraphicRaycaster
                uiObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            
            // 初始化PanelSettings管理器
            PanelSettingsManager.Instance.PreloadPanelSettings();
            
            Debug.Log("UIManager initialized");
        }
        
        /// <summary>
        /// 初始化其他系统
        /// </summary>
        private void InitializeOtherSystems()
        {
            // 在这里初始化其他游戏系统
            // 如音频管理器、资源管理器等
            
            Debug.Log("Other systems initialized");
        }
        
        /// <summary>
        /// 显示启动画面
        /// </summary>
        private IEnumerator ShowSplashScreen()
        {
            _currentPhase = LaunchPhase.SplashScreen;
            Debug.Log("Showing splash screen...");
            
            // 这里可以显示公司Logo或游戏启动画面
            // 暂时用延时模拟
            yield return new WaitForSeconds(splashScreenDuration);
            
            Debug.Log("Splash screen completed");
        }
        
        /// <summary>
        /// 显示登录界面
        /// </summary>
        private IEnumerator ShowLoginInterface()
        {
            _currentPhase = LaunchPhase.Login;
            Debug.Log("Showing login interface...");
            
            // 通过UI管理器显示登录界面
            var loginPanel = UIManager.Instance.ShowUI<LoginPanel>("LoginPanel");
            if (loginPanel == null)
            {
                Debug.LogError("Failed to show login panel");
                yield break;
            }
            
            // 如果启用了自动登录且有保存的凭证
            if (autoLoginWithSavedCredentials && HasSavedCredentials())
            {
                Debug.Log("Auto-login with saved credentials");
                // 可以在这里触发自动登录
            }
            
            Debug.Log("Login interface displayed");
        }
        
        /// <summary>
        /// 显示主菜单
        /// </summary>
        private IEnumerator ShowMainMenu()
        {
            _currentPhase = LaunchPhase.MainMenu;
            Debug.Log("Showing main menu...");
            
            // 这里实现主菜单逻辑
            yield return null;
        }
        
        /// <summary>
        /// 检查是否有保存的登录凭证
        /// </summary>
        private bool HasSavedCredentials()
        {
            return PlayerPrefs.HasKey("SavedUsername") && 
                   PlayerPrefs.HasKey("SavedPassword") &&
                   PlayerPrefs.GetInt("RememberPassword", 0) == 1;
        }
        
        /// <summary>
        /// 开始游戏加载
        /// </summary>
        public void StartGameLoading()
        {
            StartCoroutine(GameLoadingSequence());
        }
        
        /// <summary>
        /// 游戏加载序列
        /// </summary>
        private IEnumerator GameLoadingSequence()
        {
            _currentPhase = LaunchPhase.GameLoading;
            Debug.Log("Starting game loading sequence...");
            
            // 显示加载界面
            ShowLoadingInterface();
            
            // 加载游戏资源
            yield return LoadGameResources();
            
            // 连接服务器
            if (autoConnectServer)
            {
                yield return ConnectToGameServer();
            }
            
            // 初始化游戏世界
            yield return InitializeGameWorld();
            
            // 进入游戏
            EnterGame();
        }
        
        /// <summary>
        /// 显示加载界面
        /// </summary>
        private void ShowLoadingInterface()
        {
            // 实现加载界面显示逻辑
            Debug.Log("Showing loading interface...");
        }
        
        /// <summary>
        /// 加载游戏资源
        /// </summary>
        private IEnumerator LoadGameResources()
        {
            Debug.Log("Loading game resources...");
            
            // 这里实现资源加载逻辑
            // 可以使用YooAsset或其他资源管理系统
            
            yield return new WaitForSeconds(1.0f); // 模拟加载时间
            
            Debug.Log("Game resources loaded");
        }
        
        /// <summary>
        /// 连接到游戏服务器
        /// </summary>
        private IEnumerator ConnectToGameServer()
        {
            Debug.Log("Connecting to game server...");
            
            NetworkManager.Instance?.ConnectToServer();
            
            // 等待连接建立
            yield return new WaitForSeconds(1.0f);
            
            Debug.Log("Connected to game server");
        }
        
        /// <summary>
        /// 初始化游戏世界
        /// </summary>
        private IEnumerator InitializeGameWorld()
        {
            Debug.Log("Initializing game world...");
            
            // 初始化游戏世界逻辑
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("Game world initialized");
        }
        
        /// <summary>
        /// 进入游戏
        /// </summary>
        private void EnterGame()
        {
            _currentPhase = LaunchPhase.InGame;
            Debug.Log("Entering game...");
            
            // 隐藏登录界面
            UIManager.Instance?.HideUI("LoginPanel");
            
            // 加载主游戏场景
            // SceneManager.LoadScene("MainGameScene");
        }
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        private void OnApplicationQuit()
        {
            Debug.Log("Application quitting...");
            // 清理资源和保存数据
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"Application focus changed: {hasFocus}");
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"Application pause status: {pauseStatus}");
        }
    }
}