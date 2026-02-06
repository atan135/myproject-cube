using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cube.Shared.Utils;

namespace Cube.HttpServer.Controllers;

/// <summary>
/// 测试控制器
/// 用于验证认证功能
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// 公开的测试接口
    /// </summary>
    [HttpGet("public")]
    public IActionResult PublicTest()
    {
        return Ok(new { message = "这是公开接口，无需认证", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// 需要认证的测试接口
    /// </summary>
    [Authorize]
    [HttpGet("protected")]
    public IActionResult ProtectedTest()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        
        return Ok(new 
        { 
            message = "这是受保护的接口，认证成功", 
            userId = userId,
            username = username,
            timestamp = DateTime.UtcNow 
        });
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [Authorize]
    [HttpGet("userinfo")]
    public IActionResult GetUserInfo()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        
        return Ok(new 
        { 
            userId = userId,
            username = username,
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToArray()
        });
    }
}