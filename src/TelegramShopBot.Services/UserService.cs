using System.Text.RegularExpressions;
using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUnitOfWork uow) => _userRepo = uow.Users;

    public async Task<UserProfile> RegisterOrGetUserAsync(long telegramId, string? username, string firstName, string? lastName)
    {
        var user = await _userRepo.GetByTelegramIdAsync(telegramId);
        if (user != null) return user;

        user = new UserProfile
        {
            TelegramId = telegramId,
            Username = username,
            FirstName = firstName,
            LastName = lastName,
            RegistrationDate = DateTime.UtcNow,
            IsAdmin = false
        };
        return await _userRepo.CreateAsync(user);
    }

    public async Task<UserProfile?> GetUserProfileAsync(long telegramId)
    {
        return await _userRepo.GetByTelegramIdAsync(telegramId);
    }

    public async Task UpdatePhoneNumberAsync(long telegramId, string phoneNumber)
    {
        if (!ValidatePhoneNumber(phoneNumber))
            throw new ArgumentException("Invalid phone number format. Use digits with optional + prefix.");

        var user = await _userRepo.GetByTelegramIdAsync(telegramId);
        if (user == null) throw new InvalidOperationException("User not found");

        user.PhoneNumber = phoneNumber;
        await _userRepo.UpdateAsync(user);
    }

    public async Task UpdateDeliveryAddressAsync(long telegramId, string address)
    {
        var user = await _userRepo.GetByTelegramIdAsync(telegramId);
        if (user == null) throw new InvalidOperationException("User not found");

        user.DeliveryAddress = address;
        await _userRepo.UpdateAsync(user);
    }

    public bool ValidatePhoneNumber(string phoneNumber)
    {
        return Regex.IsMatch(phoneNumber, @"^\+?\d{7,15}$");
    }

    public async Task<bool> HasDeliveryAddressAsync(long telegramId)
    {
        var user = await _userRepo.GetByTelegramIdAsync(telegramId);
        return user?.DeliveryAddress != null;
    }

    public async Task<bool> HasPhoneNumberAsync(long telegramId)
    {
        var user = await _userRepo.GetByTelegramIdAsync(telegramId);
        return !string.IsNullOrEmpty(user?.PhoneNumber);
    }

    public async Task<bool> IsAdminAsync(long telegramId)
    {
        var user = await _userRepo.GetByTelegramIdAsync(telegramId);
        return user?.IsAdmin == true;
    }
}
