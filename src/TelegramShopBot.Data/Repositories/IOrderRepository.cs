using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int orderId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<List<Order>> GetByCustomerAsync(long customerId);
    Task<List<Order>> GetByStatusAsync(OrderStatus status);
    Task<List<Order>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<int> GetTotalCountAsync();
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
    Task<int> GetNextSequenceForDateAsync(string datePrefix);
    Task<List<Order>> GetFilteredAsync(
        int page, int pageSize, OrderStatus? status, DateTime? fromDate,
        DateTime? toDate, long? customerId, decimal? minAmount, decimal? maxAmount,
        string? sortBy, string? sortDirection);
    Task<int> GetFilteredCountAsync(
        OrderStatus? status, DateTime? fromDate, DateTime? toDate,
        long? customerId, decimal? minAmount, decimal? maxAmount);
}
