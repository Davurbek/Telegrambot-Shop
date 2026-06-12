using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public interface IAuditLogger
{
    Task LogUserActionAsync(long userId, string action, string? details = null);
    Task LogOrderStatusChangeAsync(int orderId, OrderStatus oldStatus, OrderStatus newStatus);
    Task LogAdminActionAsync(long adminId, string action, string? details = null);
    Task LogErrorAsync(string context, Exception? ex = null, long? userId = null);
    Task LogPaymentEventAsync(int orderId, string eventType, string? details = null);
}
