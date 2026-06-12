namespace TelegramShopBot.Domain.Models;

public class CartItem
{
    public int CartItemId { get; set; }
    public long CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public UserProfile? Customer { get; set; }
    public Product? Product { get; set; }

    public decimal TotalPrice => Product?.Price * Quantity ?? 0;
}
