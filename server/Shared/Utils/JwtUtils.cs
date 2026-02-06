using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Cube.Shared.Utils;

/// <summary>
/// JWT工具类
/// 用于生成和验证JSON Web Token
/// </summary>
public static class JwtUtils
{
    private static string? _secretKey;
    private static SymmetricSecurityKey? _securityKey;
    private static SigningCredentials? _signingCredentials;

    /// <summary>
    /// 初始化JWT配置
    /// </summary>
    public static void Initialize()
    {
        _secretKey = SimpleConfig.GetRequiredString("Jwt:SecretKey");
        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// 生成JWT Token
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="username">用户名</param>
    /// <param name="expiresInMinutes">过期时间（分钟）</param>
    /// <returns>JWT Token</returns>
    public static string GenerateToken(long userId, string username, int expiresInMinutes = 1440)
    {
        EnsureInitialized();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            SigningCredentials = _signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 验证JWT Token
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>ClaimsPrincipal对象</returns>
    public static ClaimsPrincipal? ValidateToken(string token)
    {
        EnsureInitialized();

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _securityKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从Token中提取用户ID
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>用户ID</returns>
    public static long? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        var userIdClaim = principal?.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId) ? userId : null;
    }

    /// <summary>
    /// 从Token中提取用户名
    /// </summary>
    /// <param name="token">JWT Token</param>
    /// <returns>用户名</returns>
    public static string? GetUsernameFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.Name)?.Value;
    }

    private static void EnsureInitialized()
    {
        if (_secretKey == null || _securityKey == null || _signingCredentials == null)
        {
            Initialize();
        }
    }
}