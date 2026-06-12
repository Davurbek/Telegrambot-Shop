using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface ICartRepository
{
    Task<CartItem?> GetByCustomerAndProductAsync(long customerId, int productId);
    Task<List<CartItem>> GetByCustomerAsync(long customerId);
    Task<CartItem> CreateAsync(CartItem item);
    Task UpdateAsync(CartItem item);
    Task DeleteAsync(int cartItemId);
    Task ClearByCustomerAsync(long customerId);
    Task<int> GetCountByCustomerAsync(long customerId);
}
