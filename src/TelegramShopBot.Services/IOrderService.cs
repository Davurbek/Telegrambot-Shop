using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(long customerId);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<List<Order>> GetOrderHistoryAsync(long customerId);
    Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status);
    Task ApproveOrderAsync(int orderId, string orderNumber, long adminId);
    Task RejectOrderAsync(int orderId, string orderNumber, long adminId, string reason);
    Task CancelOrderAsync(int orderId, long customerId);
    Task<Order?> MarkAsShippedAsync(int orderId, string? trackingNumber, DateTime? estimatedDelivery);
    Task<Order?> MarkAsDeliveredAsync(int orderId);
    Task<OrderStatistics> GetStatisticsAsync();
    Task<(List<Order> Items, int TotalCount)> GetOrdersFilteredAsync(
        int page, int pageSize, OrderStatus? status, DateTime? fromDate,
        DateTime? toDate, long? customerId, decimal? minAmount, decimal? maxAmount,
        string? sortBy, string? sortDirection);
    Task<List<(string ProductName, int TotalQuantitySold, decimal TotalRevenue)>> GetTopSellingProductsAsync(int count = 5);
}
