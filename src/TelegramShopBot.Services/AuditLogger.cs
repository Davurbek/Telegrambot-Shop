using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class AuditLogger : IAuditLogger
{
    private readonly IAuditLogRepository _auditRepo;

    public AuditLogger(IUnitOfWork uow) => _auditRepo = uow.AuditLogs;

    public async Task LogUserActionAsync(long userId, string action, string? details = null)
    {
        await _auditRepo.CreateAsync(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Action = action,
            Details = details
        });
    }

    public async Task LogOrderStatusChangeAsync(int orderId, OrderStatus oldStatus, OrderStatus newStatus)
    {
        await _auditRepo.CreateAsync(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            Action = "OrderStatusChanged",
            Details = $"Order {orderId}: {oldStatus} -> {newStatus}"
        });
    }

    public async Task LogAdminActionAsync(long adminId, string action, string? details = null)
    {
        await _auditRepo.CreateAsync(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = adminId,
            Action = $"Admin: {action}",
            Details = details
        });
    }

    public async Task LogErrorAsync(string context, Exception? ex = null, long? userId = null)
    {
        await _auditRepo.CreateAsync(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Action = $"Error: {context}",
            ErrorMessage = ex?.ToString()
        });
    }

    public async Task LogPaymentEventAsync(int orderId, string eventType, string? details = null)
    {
        await _auditRepo.CreateAsync(new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            Action = $"Payment: {eventType}",
            Details = $"Order {orderId}: {details}"
        });
    }
}
