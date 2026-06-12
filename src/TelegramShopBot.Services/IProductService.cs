using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface IProductService
{
    Task<Category> CreateCategoryAsync(string name, string? description);
    Task<Category?> UpdateCategoryAsync(int categoryId, string name, string? description);
    Task DeleteCategoryAsync(int categoryId);
    Task<List<Category>> GetAllCategoriesAsync();
    Task<(List<Category> Items, int TotalCount)> GetCategoriesPagedAsync(int page, int pageSize);
    Task<Category?> GetCategoryByIdAsync(int categoryId);
    Task<int> GetProductCountByCategoryAsync(int categoryId);

    Task<Product> CreateProductAsync(string name, string? description, decimal price, int categoryId, string? imageUrl, int stockQuantity);
    Task<Product?> UpdateProductAsync(int productId, string name, string? description, decimal price, int categoryId, string? imageUrl, int stockQuantity);
    Task DeleteProductAsync(int productId);
    Task<Product?> GetProductByIdAsync(int productId);

    Task<(List<Product> Items, int TotalCount)> GetProductsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10);
    Task<List<Product>> SearchProductsAsync(string query);
    Task<bool> IsProductAvailableAsync(int productId, int quantity);
    Task<int> GetTotalProductCountAsync();
}
