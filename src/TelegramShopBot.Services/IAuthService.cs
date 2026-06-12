using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface IAuthService
{
    Task<AdminUser?> ValidateCredentialsAsync(string email, string password);
    Task<AdminUser?> GetAdminByIdAsync(int id);
}
