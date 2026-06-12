using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog log);
    Task<List<AuditLog>> GetByUserIdAsync(long userId, int limit = 50);
    Task<List<AuditLog>> GetRecentAsync(int limit = 100);
}
