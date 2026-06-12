using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface IUserService
{
    Task<UserProfile> RegisterOrGetUserAsync(long telegramId, string? username, string firstName, string? lastName);
    Task<UserProfile?> GetUserProfileAsync(long telegramId);
    Task UpdatePhoneNumberAsync(long telegramId, string phoneNumber);
    Task UpdateDeliveryAddressAsync(long telegramId, string address);
    bool ValidatePhoneNumber(string phoneNumber);
    Task<bool> HasDeliveryAddressAsync(long telegramId);
    Task<bool> HasPhoneNumberAsync(long telegramId);
    Task<bool> IsAdminAsync(long telegramId);
}
