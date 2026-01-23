using Microsoft.AspNetCore.Mvc;
using Cube.Shared.Models;

namespace Cube.HttpServer.Controllers;

/// <summary>
/// 认证控制器
/// 处理用户登录、注册等认证相关请求
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // TODO: 实现登录逻辑
        return Ok(new { token = "mock_token", userId = 1 });
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        // TODO: 实现注册逻辑
        return Ok(new { success = true, userId = 1 });
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
}
