using System;
using System.Collections.Generic;

namespace entity
{
    [Serializable]
    public class LoginResponse
    {
        public int code;           // 服务端状态码 (如 200 成功)
        public string msg;         // 错误信息描述
        public string token;       // 鉴权令牌
        public UserInfo userInfo;  // 玩家基础信息
    }
    [Serializable]
    public class UserInfo
    {
        public string uid;
        public string nickname;
        public int level;
        public List<int> unlockedLevels;
    }
    [Serializable]
    public class BaseResponse<T>
    {
        public int code;      // 业务状态码 (如 200:成功, 401:Token过期, 500:服务器错误)
        public string msg;    // 提示信息
        public T data;        // 具体的业务数据 (由泛型 T 决定)
    }
    
}