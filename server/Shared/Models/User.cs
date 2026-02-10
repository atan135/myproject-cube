namespace Cube.Shared.Models;

/// <summary>
/// 用户模型
/// 对应数据库game_users表
/// </summary>
public class User
{
    public Int64 Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int Level { get; set; } = 1;
    public Int64 Experience { get; set; } = 0;
    public Int64 Coins { get; set; } = 0;
    public Int64 Diamonds { get; set; } = 0;
    public int Status { get; set; } = 1; // 1-正常, 2-封禁, 3-注销
    public DateTime? LastLoginTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

