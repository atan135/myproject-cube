using UnityEngine;
using UnityEngine.UIElements;

public class LoginController
{
    private VisualElement _root;

    public LoginController(VisualElement root)
    {
        _root = root;
        BindEvents();
    }

    private void BindEvents()
    {
        // 使用 Q (Query) 获取 UXML 中定义的元素名称
        var loginBtn = _root.Q<Button>("LoginButton");
        var userField = _root.Q<TextField>("UserField");
        var passField = _root.Q<TextField>("PasswordField");

        if (loginBtn != null)
        {
            loginBtn.clicked += () => {
                Debug.Log($"尝试登录: 用户名={userField.value}, 密码={passField.value}");
                OnClickLogin(userField.value, passField.value);
            };
        }
    }

    public async void OnClickLogin(string username, string password)
    {
        Debug.Log($"尝试登录: 用户名={username}, 密码={password}");
        // 调用逻辑
        bool isOk = await LoginModule.Instance.LoginAsync(username, password);
    
        if (isOk)
        {
            Logger.UI("登录成功，准备切换至主城场景");
            // 1. 自动加载/获取 Loading 模块
            var loading = await LoadingModule.GetInstanceAsync();
            // 2. 执行场景跳转
            await loading.LoadSceneWithProgress("Scene_MainCity");
        }
        else
        {
            // 这里的错误提示通常已经在 HttpManager 的 baseRes.msg 中由 Log.Error 打印了
            Logger.UI("登录失败，请重试");
        }
    
    }

}
