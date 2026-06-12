using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int categoryId);
    Task<List<Category>> GetAllAsync();
    Task<Category> CreateAsync(Category category);
    Task<Category> UpdateAsync(Category category);
    Task DeleteAsync(int categoryId);
    Task<int> GetProductCountAsync(int categoryId);
    Task<List<Category>> GetPagedAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
}
