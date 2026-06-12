using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUnitOfWork uow, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<AdminUser?> ValidateCredentialsAsync(string email, string password)
    {
        var admin = await _uow.AdminUsers.GetByEmailAsync(email);
        if (admin == null || !admin.IsActive) return null;

        if (!_passwordHasher.VerifyPassword(password, admin.PasswordHash)) return null;

        admin.LastLoginAt = DateTime.UtcNow;
        await _uow.AdminUsers.UpdateAsync(admin);
        return admin;
    }

    public async Task<AdminUser?> GetAdminByIdAsync(int id) =>
        await _uow.AdminUsers.GetByIdAsync(id);
}
