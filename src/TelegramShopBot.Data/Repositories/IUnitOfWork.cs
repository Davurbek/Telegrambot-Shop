using System.Data;

namespace TelegramShopBot.Data.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IOrderRepository Orders { get; }
    ICartRepository CartItems { get; }
    IPaymentRepository Payments { get; }
    IAuditLogRepository AuditLogs { get; }
    IAdminUserRepository AdminUsers { get; }
    IRefreshTokenRepository RefreshTokens { get; }

    IDbConnection? SharedConnection { get; set; }
    IDbTransaction? SharedTransaction { get; set; }
    Task<IDbTransaction> BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
