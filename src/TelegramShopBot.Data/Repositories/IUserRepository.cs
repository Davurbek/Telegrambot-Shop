using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IUserRepository
{
    Task<UserProfile?> GetByTelegramIdAsync(long telegramId);
    Task<UserProfile> CreateAsync(UserProfile user);
    Task<UserProfile> UpdateAsync(UserProfile user);
    Task<List<UserProfile>> GetAdminsAsync();
}
