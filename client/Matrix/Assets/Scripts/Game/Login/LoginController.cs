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
                
                // 模拟登录成功逻辑
                if (!string.IsNullOrEmpty(userField.value)) {
                    Debug.Log("登录成功！正在进入游戏...");
                    UIManager.Instance.CloseScreen("Login");
                }
            };
        }
    }
}
