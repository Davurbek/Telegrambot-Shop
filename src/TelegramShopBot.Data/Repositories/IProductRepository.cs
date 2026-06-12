using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int productId);
    Task<List<Product>> GetByCategoryAsync(int categoryId, int page = 1, int pageSize = 10);
    Task<int> GetCountByCategoryAsync(int categoryId);
    Task<List<Product>> SearchAsync(string query);
    Task<List<Product>> GetAllAsync();
    Task<Product> CreateAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task DeleteAsync(int productId);
    Task<int> GetTotalCountAsync();
}
