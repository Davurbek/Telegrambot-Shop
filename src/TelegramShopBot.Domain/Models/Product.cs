namespace TelegramShopBot.Domain.Models;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }

    public bool IsAvailable => StockQuantity > 0;
    public Category? Category { get; set; }
}
