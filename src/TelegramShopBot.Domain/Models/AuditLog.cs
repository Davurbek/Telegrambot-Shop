namespace TelegramShopBot.Domain.Models;

public class AuditLog
{
    public int LogId { get; set; }
    public DateTime Timestamp { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
}
