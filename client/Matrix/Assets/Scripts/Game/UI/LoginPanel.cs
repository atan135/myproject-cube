using UnityEngine;
using UnityEngine.UIElements;
using Cube.Framework.UI;
using Cube.Network;

namespace Cube.Game.UI
{
    /// <summary>
    /// 登录界面
    /// </summary>
    public class LoginPanel : UIBase
    {
        public override string UIName => "LoginPanel";
        public override int UILayer => (int)Cube.Framework.UI.UILayer.Normal;
        
        // UI元素引用
        private TextField _usernameInput;
        private TextField _passwordInput;
        private Toggle _rememberToggle;
        private Button _loginButton;
        private Button _registerButton;
        
        // 输入数据
        private string _username = "";
        private string _password = "";
        private bool _rememberPassword = false;
        
        protected override void OnInit()
        {
            base.OnInit();
            
            // 确保PanelSettings已分配
            EnsurePanelSettingsAssigned();
            
            // 查找UI元素
            FindUIElements();
            
            // 注册事件
            RegisterEvents();
            
            // 加载保存的登录信息
            LoadSavedLoginInfo();
            
            Debug.Log("LoginPanel initialized");
        }
        
        /// <summary>
        /// 确保PanelSettings已正确分配
        /// </summary>
        private void EnsurePanelSettingsAssigned()
        {
            if (_uiDocument != null && _uiDocument.panelSettings == null)
            {
                // 使用PanelSettingsManager来处理PanelSettings分配
                PanelSettingsManager.Instance.AssignPanelSettings(_uiDocument);
            }
        }
        
        protected override void OnShow()
        {
            base.OnShow();
            Debug.Log("LoginPanel shown");
        }
        
        protected override void OnHide()
        {
            base.OnHide();
            Debug.Log("LoginPanel hidden");
        }
        
        /// <summary>
        /// 查找UI元素
        /// </summary>
        private void FindUIElements()
        {
            _usernameInput = FindElement<TextField>("UsernameInput");
            _passwordInput = FindElement<TextField>("PasswordInput");
            _rememberToggle = FindElement<Toggle>("RememberToggle");
            _loginButton = FindElement<Button>("LoginButton");
            _registerButton = FindElement<Button>("RegisterButton");
            
            // 如果没找到，尝试在子元素中查找
            if (_usernameInput == null)
            {
                _usernameInput = FindElementInChildren<TextField>("UsernameInput");
            }
            
            if (_passwordInput == null)
            {
                _passwordInput = FindElementInChildren<TextField>("PasswordInput");
            }
            
            if (_loginButton == null)
            {
                _loginButton = FindElementInChildren<Button>("LoginButton");
            }
            
            // 验证元素是否存在
            if (_usernameInput == null) Debug.LogError("UsernameInput not found");
            if (_passwordInput == null) Debug.LogError("PasswordInput not found");
            if (_loginButton == null) Debug.LogError("LoginButton not found");
        }
        
        /// <summary>
        /// 在子元素中递归查找UI元素
        /// </summary>
        private T FindElementInChildren<T>(string name) where T : VisualElement
        {
            if (_root == null) return null;
            
            return FindElementRecursive<T>(_root, name);
        }
        
        /// <summary>
        /// 递归查找元素
        /// </summary>
        private T FindElementRecursive<T>(VisualElement parent, string name) where T : VisualElement
        {
            if (parent == null) return null;
            
            // 检查当前元素
            if (parent.name == name && parent is T)
            {
                return parent as T;
            }
            
            // 递归检查子元素
            foreach (var child in parent.Children())
            {
                var result = FindElementRecursive<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 注册事件
        /// </summary>
        private void RegisterEvents()
        {
            // 输入框事件
            if (_usernameInput != null)
            {
                _usernameInput.RegisterValueChangedCallback(OnUsernameChanged);
            }
            
            if (_passwordInput != null)
            {
                _passwordInput.RegisterValueChangedCallback(OnPasswordChanged);
            }
            
            if (_rememberToggle != null)
            {
                _rememberToggle.RegisterValueChangedCallback(OnRememberToggleChanged);
            }
            
            // 按钮事件
            RegisterButtonClick("LoginButton", OnLoginButtonClicked);
            RegisterButtonClick("RegisterButton", OnRegisterButtonClicked);
        }
        
        /// <summary>
        /// 用户名输入变化
        /// </summary>
        private void OnUsernameChanged(ChangeEvent<string> evt)
        {
            _username = evt.newValue;
            ValidateInput();
        }
        
        /// <summary>
        /// 密码输入变化
        /// </summary>
        private void OnPasswordChanged(ChangeEvent<string> evt)
        {
            _password = evt.newValue;
            ValidateInput();
        }
        
        /// <summary>
        /// 记住密码选项变化
        /// </summary>
        private void OnRememberToggleChanged(ChangeEvent<bool> evt)
        {
            _rememberPassword = evt.newValue;
        }
        
        /// <summary>
        /// 登录按钮点击
        /// </summary>
        private void OnLoginButtonClicked()
        {
            if (!ValidateInput())
            {
                ShowErrorMessage("请输入完整的用户名和密码");
                return;
            }
            
            // 显示加载状态
            SetLoginButtonState(true);
            
            // 执行登录逻辑
            ExecuteLogin();
        }
        
        /// <summary>
        /// 注册按钮点击
        /// </summary>
        private void OnRegisterButtonClicked()
        {
            Debug.Log("Register button clicked");
            ShowMessage("注册功能暂未开放");
        }
        
        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInput()
        {
            bool isValid = !string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password);
            
            if (_loginButton != null)
            {
                _loginButton.SetEnabled(isValid);
            }
            
            return isValid;
        }
        
        /// <summary>
        /// 执行登录
        /// </summary>
        private void ExecuteLogin()
        {
            Debug.Log($"Attempting to login with username: {_username}");
            
            // 这里应该调用网络管理器进行实际登录
            // 模拟异步登录过程
            Invoke(nameof(SimulateLoginResponse), 1.5f);
        }
        
        /// <summary>
        /// 模拟登录响应
        /// </summary>
        private void SimulateLoginResponse()
        {
            // 模拟登录成功
            bool loginSuccess = true; // 实际应该根据服务器返回结果判断
            
            if (loginSuccess)
            {
                OnLoginSuccess();
            }
            else
            {
                OnLoginFailed("用户名或密码错误");
            }
        }
        
        /// <summary>
        /// 登录成功
        /// </summary>
        private void OnLoginSuccess()
        {
            Debug.Log("Login successful");
            
            // 保存登录信息（如果选择了记住密码）
            if (_rememberPassword)
            {
                SaveLoginInfo();
            }
            
            // 显示成功消息
            ShowMessage("登录成功！");
            
            // 切换到游戏主界面或加载场景
            LoadGameScene();
            
            // 隐藏当前界面
            Hide();
        }
        
        /// <summary>
        /// 登录失败
        /// </summary>
        private void OnLoginFailed(string errorMessage)
        {
            Debug.Log($"Login failed: {errorMessage}");
            
            // 恢复登录按钮状态
            SetLoginButtonState(false);
            
            // 显示错误消息
            ShowErrorMessage(errorMessage);
        }
        
        /// <summary>
        /// 设置登录按钮状态
        /// </summary>
        private void SetLoginButtonState(bool isLoading)
        {
            if (_loginButton != null)
            {
                _loginButton.SetEnabled(!isLoading);
                _loginButton.text = isLoading ? "登录中..." : "登录游戏";
            }
        }
        
        /// <summary>
        /// 加载游戏场景
        /// </summary>
        private void LoadGameScene()
        {
            // 这里应该加载主游戏场景
            Debug.Log("Loading game scene...");
            
            // 示例：连接到服务器
            NetworkManager.Instance?.ConnectToServer();
        }
        
        /// <summary>
        /// 保存登录信息
        /// </summary>
        private void SaveLoginInfo()
        {
            PlayerPrefs.SetString("SavedUsername", _username);
            PlayerPrefs.SetString("SavedPassword", _password); // 实际项目中应该加密存储
            PlayerPrefs.SetInt("RememberPassword", _rememberPassword ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// 加载保存的登录信息
        /// </summary>
        private void LoadSavedLoginInfo()
        {
            if (PlayerPrefs.HasKey("SavedUsername"))
            {
                string savedUsername = PlayerPrefs.GetString("SavedUsername");
                string savedPassword = PlayerPrefs.GetString("SavedPassword");
                int remember = PlayerPrefs.GetInt("RememberPassword", 0);
                
                _username = savedUsername;
                _password = savedPassword;
                _rememberPassword = remember == 1;
                
                // 更新UI
                if (_usernameInput != null) _usernameInput.value = _username;
                if (_passwordInput != null) _passwordInput.value = _password;
                if (_rememberToggle != null) _rememberToggle.value = _rememberPassword;
                
                ValidateInput();
            }
        }
        
        /// <summary>
        /// 显示普通消息
        /// </summary>
        private void ShowMessage(string message)
        {
            Debug.Log($"[LoginPanel] {message}");
            // 这里可以调用消息提示系统
        }
        
        /// <summary>
        /// 显示错误消息
        /// </summary>
        private void ShowErrorMessage(string message)
        {
            Debug.LogError($"[LoginPanel] {message}");
            // 这里可以调用错误提示系统
        }
    }
}