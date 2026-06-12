using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow) => _uow = uow;

    public async Task AddToCartAsync(long customerId, int productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

        var product = await _uow.Products.GetByIdAsync(productId);
        if (product == null) throw new InvalidOperationException("Product not found");

        var existing = await _uow.CartItems.GetByCustomerAndProductAsync(customerId, productId);
        var totalQty = (existing?.Quantity ?? 0) + quantity;

        if (totalQty > product.StockQuantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}, requested: {totalQty}");

        if (existing != null)
        {
            existing.Quantity = totalQty;
            await _uow.CartItems.UpdateAsync(existing);
        }
        else
        {
            await _uow.CartItems.CreateAsync(new CartItem
            {
                CustomerId = customerId,
                ProductId = productId,
                Quantity = quantity
            });
        }
    }

    public async Task UpdateCartItemQuantityAsync(long customerId, int productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive");

        var existing = await _uow.CartItems.GetByCustomerAndProductAsync(customerId, productId);
        if (existing == null) throw new InvalidOperationException("Item not in cart");

        var product = await _uow.Products.GetByIdAsync(productId);
        if (product == null) throw new InvalidOperationException("Product not found");

        if (quantity > product.StockQuantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {product.StockQuantity}");

        existing.Quantity = quantity;
        await _uow.CartItems.UpdateAsync(existing);
    }

    public async Task RemoveFromCartAsync(long customerId, int productId)
    {
        var item = await _uow.CartItems.GetByCustomerAndProductAsync(customerId, productId);
        if (item != null)
            await _uow.CartItems.DeleteAsync(item.CartItemId);
    }

    public async Task<List<CartItem>> GetCartItemsAsync(long customerId) =>
        await _uow.CartItems.GetByCustomerAsync(customerId);

    public async Task<decimal> GetCartTotalAsync(long customerId)
    {
        var items = await _uow.CartItems.GetByCustomerAsync(customerId);
        return items.Sum(i => i.TotalPrice);
    }

    public async Task ClearCartAsync(long customerId) =>
        await _uow.CartItems.ClearByCustomerAsync(customerId);

    public async Task<bool> IsCartEmptyAsync(long customerId) =>
        await _uow.CartItems.GetCountByCustomerAsync(customerId) == 0;

    public async Task<bool> ValidateCartStockAsync(long customerId)
    {
        var items = await _uow.CartItems.GetByCustomerAsync(customerId);
        foreach (var item in items)
        {
            if (item.Product == null) continue;
            if (item.Quantity > item.Product.StockQuantity) return false;
        }
        return true;
    }
}
