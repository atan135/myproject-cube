using Microsoft.AspNetCore.Mvc;
using Cube.Shared.Models;
using Cube.Shared.Repositories;
using Cube.Shared.Utils;
using System.ComponentModel.DataAnnotations;

namespace Cube.HttpServer.Controllers;

/// <summary>
/// 认证控制器
/// 处理用户登录、注册等认证相关请求
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _userRepository;
    private readonly Database _database;

    public AuthController(Database database)
    {
        _database = database;
        _userRepository = new UserRepository(database);
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "用户名和密码不能为空" });
            }

            await _database.OpenAsync();
            
            // 查找用户
            var user = await _userRepository.GetUserByUsernameAsync(request.Username);
            if (user == null)
            {
                // 记录失败的登录尝试
                await _userRepository.CreateLoginRecordAsync(0, GetClientIp(), Request.Headers.UserAgent, false, "用户不存在");
                return Unauthorized(new { error = "用户名或密码错误" });
            }

            // 检查账户状态
            if (user.Status != 1)
            {
                await _userRepository.CreateLoginRecordAsync(user.Id, GetClientIp(), Request.Headers.UserAgent, false, "账户状态异常");
                return Unauthorized(new { error = "账户已被禁用或注销" });
            }

            // 验证密码
            if (!UserRepository.VerifyPassword(request.Password, user.PasswordHash))
            {
                await _userRepository.CreateLoginRecordAsync(user.Id, GetClientIp(), Request.Headers.UserAgent, false, "密码错误");
                return Unauthorized(new { error = "用户名或密码错误" });
            }

            // 生成JWT Token
            var token = JwtUtils.GenerateToken(user.Id, user.Username);
            var expiresAt = DateTime.UtcNow.AddMinutes(1440); // 24小时过期

            // 更新最后登录时间
            await _userRepository.UpdateLastLoginTimeAsync(user.Id, DateTime.UtcNow);

            // 记录成功的登录
            await _userRepository.CreateLoginRecordAsync(user.Id, GetClientIp(), Request.Headers.UserAgent, true);

            // 返回登录响应
            var response = new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                Email = user.Email,
                Level = user.Level,
                Experience = user.Experience,
                Coins = user.Coins,
                Diamonds = user.Diamonds,
                Token = token,
                ExpiresAt = expiresAt
            };

            LogUtils.Info($"User {user.Username} logged in successfully from IP: {GetClientIp()}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            LogUtils.Error("Login failed", ex);
            return StatusCode(500, new { error = "服务器内部错误" });
        }
        finally
        {
            await _database.CloseAsync();
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // 验证输入
            var validationErrors = ValidateRegisterRequest(request);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            await _database.OpenAsync();

            // 检查用户名是否已存在
            if (await _userRepository.IsUsernameExistsAsync(request.Username))
            {
                return BadRequest(new { error = "用户名已存在" });
            }

            // 检查邮箱是否已存在
            if (await _userRepository.IsEmailExistsAsync(request.Email))
            {
                return BadRequest(new { error = "邮箱已被注册" });
            }

            // 创建用户对象
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = UserRepository.HashPassword(request.Password),
                Nickname = request.Nickname,
                AvatarUrl = null,
                Level = 1,
                Experience = 0,
                Coins = 1000, // 新用户赠送1000游戏币
                Diamonds = 10, // 新用户赠送10钻石
                Status = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 保存用户
            var userId = await _userRepository.CreateUserAsync(user);
            user.Id = userId;

            LogUtils.Info($"New user registered: {user.Username} (ID: {userId})");

            // 返回注册响应
            var response = new RegisterResponse
            {
                UserId = userId,
                Username = user.Username,
                Email = user.Email,
                Nickname = user.Nickname,
                CreatedAt = user.CreatedAt
            };

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            LogUtils.Error("Registration failed", ex);
            return StatusCode(500, new { error = "服务器内部错误" });
        }
        finally
        {
            await _database.CloseAsync();
        }
    }

    /// <summary>
    /// 验证注册请求
    /// </summary>
    private List<string> ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add("用户名不能为空");
        }
        else if (request.Username.Length < 3 || request.Username.Length > 50)
        {
            errors.Add("用户名长度必须在3-50个字符之间");
        }
        else if (!System.Text.RegularExpressions.Regex.IsMatch(request.Username, "^[a-zA-Z0-9_]+$"))
        {
            errors.Add("用户名只能包含字母、数字和下划线");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("邮箱不能为空");
        }
        else if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            errors.Add("邮箱格式不正确");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("密码不能为空");
        }
        else if (request.Password.Length < 6)
        {
            errors.Add("密码长度不能少于6个字符");
        }

        return errors;
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIp()
    {
        // 检查X-Forwarded-For头部（用于反向代理场景）
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 检查X-Real-IP头部
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 使用RemoteIpAddress
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// 登录请求模型
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 注册请求模型
/// </summary>
public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Nickname { get; set; }
}
