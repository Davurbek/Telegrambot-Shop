namespace TelegramShopBot.Domain.Models;

public class UserProfile
{
    public long TelegramId { get; set; }
    public string? Username { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime RegistrationDate { get; set; }
    public bool IsAdmin { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
