using Microsoft.AspNetCore.Mvc;

namespace Cube.HttpServer.Controllers;

/// <summary>
/// 交易控制器
/// 处理游戏内的交易、商店购买等请求
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TradeController : ControllerBase
{
    /// <summary>
    /// 购买物品
    /// </summary>
    [HttpPost("purchase")]
    public IActionResult Purchase([FromBody] PurchaseRequest request)
    {
        // TODO: 实现购买逻辑
        return Ok(new { success = true, itemId = request.ItemId });
    }

    /// <summary>
    /// 获取商店物品列表
    /// </summary>
    [HttpGet("shop")]
    public IActionResult GetShopItems()
    {
        // TODO: 实现获取商店物品逻辑
        return Ok(new { items = new List<object>() });
    }
}

/// <summary>
/// 购买请求模型
/// </summary>
public class PurchaseRequest
{
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
}
