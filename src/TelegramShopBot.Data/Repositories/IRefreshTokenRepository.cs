using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken token);
    Task RevokeAsync(int tokenId);
    Task RevokeAllForAdminAsync(int adminId);
}
