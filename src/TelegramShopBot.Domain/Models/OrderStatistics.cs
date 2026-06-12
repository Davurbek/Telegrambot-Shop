namespace TelegramShopBot.Domain.Models;

public class OrderStatistics
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<OrderStatus, int> OrdersByStatus { get; set; } = new();
}
