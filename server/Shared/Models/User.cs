namespace Cube.Shared.Models;

/// <summary>
/// 用户模型
/// 对应数据库game_users表
/// </summary>
public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    public long Coins { get; set; } = 0;
    public long Diamonds { get; set; } = 0;
    public int Status { get; set; } = 1; // 1-正常, 2-封禁, 3-注销
    public DateTime? LastLoginTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 登录响应模型
/// </summary>
public class LoginResponse
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public long Experience { get; set; }
    public long Coins { get; set; }
    public long Diamonds { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// 注册响应模型
/// </summary>
public class RegisterResponse
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public DateTime CreatedAt { get; set; }
}
