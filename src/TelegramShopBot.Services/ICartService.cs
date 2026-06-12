using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface ICartService
{
    Task AddToCartAsync(long customerId, int productId, int quantity);
    Task UpdateCartItemQuantityAsync(long customerId, int productId, int quantity);
    Task RemoveFromCartAsync(long customerId, int productId);
    Task<List<CartItem>> GetCartItemsAsync(long customerId);
    Task<decimal> GetCartTotalAsync(long customerId);
    Task ClearCartAsync(long customerId);
    Task<bool> IsCartEmptyAsync(long customerId);
    Task<bool> ValidateCartStockAsync(long customerId);
}
