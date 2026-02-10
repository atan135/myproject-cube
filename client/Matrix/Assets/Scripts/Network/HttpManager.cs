using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Matrix.Shared;

public class HttpManager : MonoBehaviour
{
    public static HttpManager Instance { get; private set; }

    [Header("配置项")]
    public string BaseUrl = "https://api.yourgame.com";
    public int TimeoutSeconds = 10;
    
    // 登录成功后存储此 Token
    public string AuthToken { get; set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 通用 POST 请求
    /// </summary>
    /// <typeparam name="T">期望返回的数据模型</typeparam>
    /// <param name="endPoint">接口路径 (如 /user/login)</param>
    /// <param name="data">发送的匿名对象或实体类</param>
    // 在 HttpManager.cs 中修改 PostAsync
    public async Task<T> PostAsync<T>(string endPoint, object data)
    {
        string url = BaseUrl + endPoint;
        string json = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        Logger.Http($"{url} \n {json}");
        using UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = TimeoutSeconds;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json"); 

        if (!string.IsNullOrEmpty(AuthToken))
            request.SetRequestHeader("Authorization", $"Bearer {AuthToken}");

        try
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();
            Logger.Http($"[响应] {request.result} {request.responseCode} {endPoint}: {request.downloadHandler.text}");
            if (request.result == UnityWebRequest.Result.Success)
            {
                // 1. 先解析成基础协议格式
                var baseRes = JsonConvert.DeserializeObject<BaseResponse<T>>(request.downloadHandler.text);
                Logger.Http($"[响应] {endPoint}: {baseRes.Msg}"); 
                // 2. 统一处理业务状态码
                if (baseRes.Code == 200) 
                {
                    return baseRes.Data; // 直接返回业务需要的 T 数据
                }
                else if (baseRes.Code == 401)
                {
                    Debug.LogWarning("Token过期，请重新登录");
                    // TODO: 可以在这里触发全局事件回到登录界面
                    return default;
                }
                else 
                {
                    Debug.LogError($"[业务错误] {endPoint}: {baseRes.Msg}");
                    // TODO: 可以在这里调用 UIManager.ShowTips(baseRes.msg)
                    return default;
                }
            }
            return default;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return default;
        }
        return default;
    }
    

}
