using System.Security.Claims;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(AdminUser admin);
    Task<RefreshToken> GenerateRefreshTokenAsync(int adminId);
    ClaimsPrincipal? ValidateToken(string token);
}
