using Cube.Shared.Models;
using Cube.Shared.Utils;
using System.Security.Cryptography;
using System.Text;

namespace Cube.Shared.Repositories;

/// <summary>
/// 用户仓储类
/// 负责用户相关的数据库操作
/// </summary>
public class UserRepository
{
    private readonly Database _database;

    public UserRepository(Database database)
    {
        _database = database;
    }

    /// <summary>
    /// 根据用户名查找用户
    /// </summary>
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT id, username, email, password_hash, nickname, avatar_url, 
                   level, experience, coins, diamonds, status, 
                   last_login_time, created_at, updated_at
            FROM game_users 
            WHERE username = @username";

        var parameters = new Dictionary<string, object> { { "@username", username } };

        try
        {
            using var reader = await _database.ExecuteReaderAsync(sql, parameters);
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to get user by username: {username}", ex);
            throw;
        }
    }

    /// <summary>
    /// 根据邮箱查找用户
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = @"
            SELECT id, username, email, password_hash, nickname, avatar_url, 
                   level, experience, coins, diamonds, status, 
                   last_login_time, created_at, updated_at
            FROM game_users 
            WHERE email = @email";

        var parameters = new Dictionary<string, object> { { "@email", email } };

        try
        {
            using var reader = await _database.ExecuteReaderAsync(sql, parameters);
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to get user by email: {email}", ex);
            throw;
        }
    }

    /// <summary>
    /// 根据用户ID查找用户
    /// </summary>
    public async Task<User?> GetUserByIdAsync(long userId)
    {
        const string sql = @"
            SELECT id, username, email, password_hash, nickname, avatar_url, 
                   level, experience, coins, diamonds, status, 
                   last_login_time, created_at, updated_at
            FROM game_users 
            WHERE id = @userId";

        var parameters = new Dictionary<string, object> { { "@userId", userId } };

        try
        {
            using var reader = await _database.ExecuteReaderAsync(sql, parameters);
            if (await reader.ReadAsync())
            {
                return MapUserFromReader(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to get user by ID: {userId}", ex);
            throw;
        }
    }

    /// <summary>
    /// 创建新用户
    /// </summary>
    public async Task<long> CreateUserAsync(User user)
    {
        const string sql = @"
            INSERT INTO game_users (username, email, password_hash, nickname, avatar_url, 
                                  level, experience, coins, diamonds, status, created_at, updated_at)
            VALUES (@username, @email, @passwordHash, @nickname, @avatarUrl, 
                    @level, @experience, @coins, @diamonds, @status, @createdAt, @updatedAt)";

        var parameters = new Dictionary<string, object>
        {
            { "@username", user.Username },
            { "@email", user.Email },
            { "@passwordHash", user.PasswordHash },
            { "@nickname", user.Nickname ?? (object)DBNull.Value },
            { "@avatarUrl", user.AvatarUrl ?? (object)DBNull.Value },
            { "@level", user.Level },
            { "@experience", user.Experience },
            { "@coins", user.Coins },
            { "@diamonds", user.Diamonds },
            { "@status", user.Status },
            { "@createdAt", user.CreatedAt },
            { "@updatedAt", user.UpdatedAt }
        };

        try
        {
            await _database.ExecuteNonQueryAsync(sql, parameters);
            
            // 获取插入的自增ID
            var lastInsertId = await _database.ExecuteScalarAsync("SELECT LAST_INSERT_ID()");
            return Convert.ToInt64(lastInsertId);
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to create user: {user.Username}", ex);
            throw;
        }
    }

    /// <summary>
    /// 更新用户最后登录时间
    /// </summary>
    public async Task UpdateLastLoginTimeAsync(long userId, DateTime loginTime)
    {
        const string sql = "UPDATE game_users SET last_login_time = @loginTime, updated_at = @updatedAt WHERE id = @userId";
        
        var parameters = new Dictionary<string, object>
        {
            { "@userId", userId },
            { "@loginTime", loginTime },
            { "@updatedAt", DateTime.UtcNow }
        };

        try
        {
            await _database.ExecuteNonQueryAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to update last login time for user: {userId}", ex);
            throw;
        }
    }

    /// <summary>
    /// 检查用户名是否已存在
    /// </summary>
    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        const string sql = "SELECT COUNT(*) FROM game_users WHERE username = @username";
        var parameters = new Dictionary<string, object> { { "@username", username } };

        try
        {
            var result = await _database.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to check username existence: {username}", ex);
            throw;
        }
    }

    /// <summary>
    /// 检查邮箱是否已存在
    /// </summary>
    public async Task<bool> IsEmailExistsAsync(string email)
    {
        const string sql = "SELECT COUNT(*) FROM game_users WHERE email = @email";
        var parameters = new Dictionary<string, object> { { "@email", email } };

        try
        {
            var result = await _database.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to check email existence: {email}", ex);
            throw;
        }
    }

    /// <summary>
    /// 创建登录记录
    /// </summary>
    public async Task CreateLoginRecordAsync(long userId, string? ipAddress, string? userAgent, bool isSuccess, string? failureReason = null)
    {
        const string sql = @"
            INSERT INTO login_records (user_id, login_ip, user_agent, login_time, login_result, failure_reason)
            VALUES (@userId, @ipAddress, @userAgent, @loginTime, @loginResult, @failureReason)";

        var parameters = new Dictionary<string, object>
        {
            { "@userId", userId },
            { "@ipAddress", ipAddress ?? (object)DBNull.Value },
            { "@userAgent", userAgent ?? (object)DBNull.Value },
            { "@loginTime", DateTime.UtcNow },
            { "@loginResult", isSuccess ? 1 : 2 }, // 1-成功, 2-失败
            { "@failureReason", failureReason ?? (object)DBNull.Value }
        };

        try
        {
            await _database.ExecuteNonQueryAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to create login record for user: {userId}", ex);
            // 登录记录不是核心功能，失败时不抛出异常
        }
    }

    /// <summary>
    /// 从DataReader映射User对象
    /// </summary>
    private static User MapUserFromReader(MySqlConnector.MySqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt64("id"),
            Username = reader.GetString("username"),
            Email = reader.GetString("email"),
            PasswordHash = reader.GetString("password_hash"),
            Nickname = reader.IsDBNull(reader.GetOrdinal("nickname")) ? null : reader.GetString("nickname"),
            AvatarUrl = reader.IsDBNull(reader.GetOrdinal("avatar_url")) ? null : reader.GetString("avatar_url"),
            Level = reader.GetInt32("level"),
            Experience = reader.GetInt64("experience"),
            Coins = reader.GetInt64("coins"),
            Diamonds = reader.GetInt64("diamonds"),
            Status = reader.GetInt32("status"),
            LastLoginTime = reader.IsDBNull(reader.GetOrdinal("last_login_time")) ? null : reader.GetDateTime("last_login_time"),
            CreatedAt = reader.GetDateTime("created_at"),
            UpdatedAt = reader.GetDateTime("updated_at")
        };
    }

    /// <summary>
    /// 密码加密
    /// </summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }
}