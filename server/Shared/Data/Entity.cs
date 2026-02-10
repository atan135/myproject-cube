using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Matrix.Shared
{
    // 统一响应包装
    public class BaseResponse<T>
    {
        [JsonProperty("code")] public int Code { get; set; } = 0;
        [JsonProperty("msg")] public string Msg { get; set; } = string.Empty;
        [JsonProperty("data")] public T? Data { get; set; } = default;

        // 快捷生成成功响应（后端用）
        public static BaseResponse<T> Success(T data, string msg = "success") 
            => new BaseResponse<T> { Code = 200, Msg = msg, Data = data };

        // 快捷生成错误响应（后端用）
        public static BaseResponse<T> Fail(int code, string msg) 
            => new BaseResponse<T> { Code = code, Msg = msg, Data = default };
    }

}
