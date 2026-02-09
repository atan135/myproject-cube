using UnityEngine;

public class GameStart : MonoBehaviour
{
    void Start()
    {
        // 1. 确保 UIManager 已经初始化 (单例模式)
        if (UIManager.Instance == null)
        {
            Debug.LogError("场景中缺少 UIManager 实例！");
            return;
        }

        // 2. 调用 UIManager 打开登录界面
        // 这里的 "Login" 必须与你在 UIManager Inspector 中设置的 Screen Name 一致
        var loginView = UIManager.Instance.OpenScreen("Login");

        if (loginView != null)
        {
            // 3. 将 UI 视图交给控制器处理逻辑
            new LoginController(loginView);
            Debug.Log("游戏启动：登录界面已就绪");
        }
    }
}
