using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Matrix.Shared
{
    public class LoginRequest
    {
        [JsonProperty("username")] public string UserName { get; set; } = string.Empty;
        [JsonProperty("password")] public string Password { get; set; } = string.Empty;
        [JsonProperty("device")] public string Device { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        [JsonProperty("token")] public string Token { get; set; } = string.Empty;
        [JsonProperty("user_info")] public UserInfo UserInfo { get; set; } = new UserInfo();
    }

    public class UserInfo
    {
        [JsonProperty("userid")] public Int64 UserId { get; set; } = 0;
        [JsonProperty("nickname")] public string Nickname { get; set; } = string.Empty;
        [JsonProperty("email")] public string Email { get; set; } = string.Empty;
        [JsonProperty("avatar")] public string Avatar { get; set; } = string.Empty;
        [JsonProperty("level")] public int Level { get; set; } = 0;
        [JsonProperty("experience")] public Int64 Experience { get; set; } = 0;
        [JsonProperty("coins")] public Int64 Coins { get; set; } = 0;
        [JsonProperty("diamonds")] public Int64 Diamonds { get; set; } = 0;
        [JsonProperty("expires_at")] public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
    }

    public class RegisterResponse
    {
        [JsonProperty("userid")] public Int64 UserId { get; set; } = 0;
        [JsonProperty("username")] public string Username { get; set; } = string.Empty;
        [JsonProperty("email")] public string Email { get; set; } = string.Empty;
        [JsonProperty("nickname")] public string? Nickname { get; set; }
        [JsonProperty("createdat")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class RegisterRequest
    {
        [JsonProperty("username")] public string Username { get; set; } = string.Empty;
        [JsonProperty("email")] public string Email { get; set; } = string.Empty;
        [JsonProperty("password")] public string Password { get; set; } = string.Empty;
        [JsonProperty("nickname")] public string? Nickname { get; set; }
    }
}

