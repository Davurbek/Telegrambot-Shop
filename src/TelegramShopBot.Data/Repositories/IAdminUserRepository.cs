using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByIdAsync(int id);
    Task<AdminUser?> GetByEmailAsync(string email);
    Task<AdminUser> CreateAsync(AdminUser admin);
    Task UpdateAsync(AdminUser admin);
    Task<List<AdminUser>> GetAllActiveAsync();
}
