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
            // 记录收到的登录请求参数（密码脱敏）
            var maskedPassword = request.password.Length > 2 ? 
                request.password.Substring(0, 2) + "***" : "***";
            LogUtils.Info($"收到登录请求 - 用户名: {request.username}, 密码: {maskedPassword}, 客户端IP: {GetClientIp()}, UserAgent: {Request.Headers.UserAgent} device: {request.device}");
            
            // 验证输入
            if (string.IsNullOrWhiteSpace(request.username) || string.IsNullOrWhiteSpace(request.password))
            {
                LogUtils.Warning($"登录验证失败 - 用户名或密码为空, 用户名: {request.username}");
                return BadRequest(new { error = "用户名和密码不能为空" });
            }

            await _database.OpenAsync();
            
            // 查找用户
            LogUtils.Info($"正在查询用户: {request.username}");
            var user = await _userRepository.GetUserByUsernameAsync(request.username);
            if (user == null)
            {
                LogUtils.Warning($"登录失败 - 用户不存在: {request.username}, IP: {GetClientIp()}");
                // 记录失败的登录尝试
                await _userRepository.CreateLoginRecordAsync(0, GetClientIp(), Request.Headers.UserAgent, false, "用户不存在");
                return Unauthorized(new { error = "用户名或密码错误" });
            }

            // 检查账户状态
            LogUtils.Info($"检查用户账户状态 - 用户名: {user.Username}, 状态: {user.Status}");
            if (user.Status != 1)
            {
                LogUtils.Warning($"登录失败 - 账户状态异常: {user.Username}(ID:{user.Id}), 状态: {user.Status}, IP: {GetClientIp()}");
                await _userRepository.CreateLoginRecordAsync(user.Id, GetClientIp(), Request.Headers.UserAgent, false, "账户状态异常");
                return Unauthorized(new { error = "账户已被禁用或注销" });
            }

            // 验证密码
            LogUtils.Info($"验证用户密码 - 用户名: {user.Username}");
            if (!UserRepository.VerifyPassword(request.password, user.PasswordHash))
            {
                LogUtils.Warning($"登录失败 - 密码验证失败: {user.Username}(ID:{user.Id}), IP: {GetClientIp()}");
                await _userRepository.CreateLoginRecordAsync(user.Id, GetClientIp(), Request.Headers.UserAgent, false, "密码错误");
                return Unauthorized(new { error = "用户名或密码错误" });
            }

            // 生成JWT Token
            LogUtils.Info($"生成JWT Token - 用户: {user.Username}(ID:{user.Id})");
            var token = JwtUtils.GenerateToken(user.Id, user.Username);
            var expiresAt = DateTime.UtcNow.AddMinutes(1440); // 24小时过期

            // 更新最后登录时间
            LogUtils.Info($"更新最后登录时间 - 用户: {user.Username}(ID:{user.Id})");
            await _userRepository.UpdateLastLoginTimeAsync(user.Id, DateTime.UtcNow);

            // 记录成功的登录
            LogUtils.Info($"记录登录日志 - 用户: {user.Username}(ID:{user.Id}), IP: {GetClientIp()}");
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

            LogUtils.Info($"登录成功 - 用户: {user.Username}(ID:{user.Id}), 昵称: {user.Nickname}, 等级: {user.Level}, 游戏币: {user.Coins}, 钻石: {user.Diamonds}, IP: {GetClientIp()}");
            return Ok(response);
        }
        catch (Exception ex)
        {
            LogUtils.Error($"登录过程发生异常 - 用户名: {request?.username ?? "未知"}, IP: {GetClientIp()}", ex);
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
            // 记录注册请求参数（密码脱敏）
            var maskedPassword = request.Password.Length > 2 ? 
                request.Password.Substring(0, 2) + "***" : "***";
            LogUtils.Info($"收到注册请求 - 用户名: {request.Username}, 邮箱: {request.Email}, 密码: {maskedPassword}, 昵称: {request.Nickname ?? "未设置"}, 客户端IP: {GetClientIp()}, UserAgent: {Request.Headers.UserAgent}");
            
            // 验证输入
            var validationErrors = ValidateRegisterRequest(request);
            if (validationErrors.Any())
            {
                LogUtils.Warning($"注册验证失败 - 用户名: {request.Username}, 错误数: {validationErrors.Count}");
                return BadRequest(new { errors = validationErrors });
            }

            await _database.OpenAsync();

            // 检查用户名是否已存在
            LogUtils.Info($"检查用户名是否存在: {request.Username}");
            if (await _userRepository.IsUsernameExistsAsync(request.Username))
            {
                LogUtils.Warning($"注册失败 - 用户名已存在: {request.Username}, IP: {GetClientIp()}");
                return BadRequest(new { error = "用户名已存在" });
            }

            // 检查邮箱是否已存在
            LogUtils.Info($"检查邮箱是否存在: {request.Email}");
            if (await _userRepository.IsEmailExistsAsync(request.Email))
            {
                LogUtils.Warning($"注册失败 - 邮箱已被注册: {request.Email}, 用户名: {request.Username}, IP: {GetClientIp()}");
                return BadRequest(new { error = "邮箱已被注册" });
            }

                        // 创建用户对象
            LogUtils.Info($"创建新用户对象 - 用户名: {request.Username}");
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
            LogUtils.Info($"保存新用户到数据库 - 用户名: {user.Username}");
            var userId = await _userRepository.CreateUserAsync(user);
            user.Id = userId;

            LogUtils.Info($"注册成功 - 用户: {user.Username}(ID:{userId}), 邮箱: {user.Email}, 昵称: {user.Nickname ?? "未设置"}, 初始游戏币: {user.Coins}, 初始钻石: {user.Diamonds}, IP: {GetClientIp()}");

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
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
    public string device { get; set; } = string.Empty;
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
