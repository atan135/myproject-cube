using System;
using System.Threading.Tasks;
using UnityEngine;

public class LoginModule
{
    private static LoginModule _instance;
    public static LoginModule Instance => _instance ??= new LoginModule();

    // 当前登录的用户信息
    public UserInfo CurrentUser { get; private set; }
    
    // 是否已登录的快捷判断
    public bool IsLoggedIn => CurrentUser != null;

    private const string SAVE_TOKEN_KEY = "LocalAuthToken";

    /// <summary>
    /// 登录业务主逻辑
    /// </summary>
    public async Task<bool> LoginAsync(string account, string password)
    {
        Logger.Http($"开始尝试登录: {account}");

        // 构造发送给服务端的参数
        var loginParams = new
        {
            username = account,
            password = password,
            device = SystemInfo.deviceModel
        };

        // 调用重构后的 HttpManager
        // 注意：这里的 LoginData 对应 BaseResponse<T> 中的 T
        var data = await HttpManager.Instance.PostAsync<LoginData>("/api/auth/login", loginParams);

        if (data != null)
        {
            HandleLoginSuccess(data);
            return true;
        }

        Logger.Error("登录流程异常，请检查网络或账号密码");
        return false;
    }

    /// <summary>
    /// 处理登录成功后的数据分配
    /// </summary>
    private void HandleLoginSuccess(LoginData data)
    {
        // 1. 同步 Token 到网络层
        HttpManager.Instance.AuthToken = data.token;
        
        // 2. 缓存用户信息
        this.CurrentUser = data.userInfo;

        // 3. 持久化 Token (用于下次自动登录)
        PlayerPrefs.SetString(SAVE_TOKEN_KEY, data.token);
        PlayerPrefs.Save();

        Logger.Http($"<color=lime>登录成功!</color> 欢迎玩家: {CurrentUser.nickname} (UID: {CurrentUser.uid})");
    }

    /// <summary>
    /// 登出逻辑
    /// </summary>
    public void Logout()
    {
        CurrentUser = null;
        HttpManager.Instance.AuthToken = string.Empty;
        PlayerPrefs.DeleteKey(SAVE_TOKEN_KEY);
        Logger.Info("已退出登录，清除本地缓存。");
    }
}

#region 数据实体 (根据服务端 JSON 定义)

[Serializable]
public class LoginData
{
    public string token;
    public UserInfo userInfo;
}

[Serializable]
public class UserInfo
{
    public string uid;
    public string nickname;
    public int level;
    public int gold;
}

#endregion
