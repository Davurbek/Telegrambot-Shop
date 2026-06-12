using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;

    public DashboardController(IOrderService orderService, IProductService productService)
    {
        _orderService = orderService;
        _productService = productService;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<object>>> GetStatistics()
    {
        var stats = await _orderService.GetStatisticsAsync();
        var productCount = await _productService.GetTotalProductCountAsync();
        var topProducts = await _orderService.GetTopSellingProductsAsync(5);

        var avgOrderValue = stats.TotalOrders > 0
            ? stats.TotalRevenue / stats.TotalOrders
            : 0;

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            stats.TotalOrders,
            stats.TotalRevenue,
            PendingOrdersCount = stats.OrdersByStatus.GetValueOrDefault(Domain.Models.OrderStatus.Pending, 0),
            TotalProducts = productCount,
            AverageOrderValue = avgOrderValue,
            OrdersByStatus = stats.OrdersByStatus.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            TopSellingProducts = topProducts.Select(p => new
            {
                p.ProductName,
                p.TotalQuantitySold,
                p.TotalRevenue
            })
        }));
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<object>>> GetRevenueTrend(
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var stats = await _orderService.GetStatisticsAsync();

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            TotalRevenue = stats.TotalRevenue,
            Period = new { From = from, To = to }
        }));
    }

    [HttpGet("orders-trend")]
    public async Task<ActionResult<ApiResponse<object>>> GetOrdersTrend(
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var stats = await _orderService.GetStatisticsAsync();

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            stats.TotalOrders,
            OrdersByStatus = stats.OrdersByStatus.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            Period = new { From = from, To = to }
        }));
    }
}
