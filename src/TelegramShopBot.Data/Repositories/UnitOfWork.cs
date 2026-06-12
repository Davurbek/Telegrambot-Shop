using System.Data;
using Microsoft.Data.SqlClient;

namespace TelegramShopBot.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly string _connectionString;
    private IDbConnection? _connection;

    public IUserRepository Users { get; }
    public IProductRepository Products { get; }
    public ICategoryRepository Categories { get; }
    public IOrderRepository Orders { get; }
    public ICartRepository CartItems { get; }
    public IPaymentRepository Payments { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IAdminUserRepository AdminUsers { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public IDbConnection? SharedConnection
    {
        get => _connection;
        set
        {
            if (value != _connection)
            {
                _connection?.Dispose();
                _connection = value;
            }
        }
    }

    public IDbTransaction? SharedTransaction { get; set; }

    public UnitOfWork(string connectionString)
    {
        _connectionString = connectionString;
        Users = new UserRepository(this, connectionString);
        Products = new ProductRepository(this, connectionString);
        Categories = new CategoryRepository(this, connectionString);
        Orders = new OrderRepository(this, connectionString);
        CartItems = new CartRepository(this, connectionString);
        Payments = new PaymentRepository(this, connectionString);
        AuditLogs = new AuditLogRepository(this, connectionString);
        AdminUsers = new AdminUserRepository(this, connectionString);
        RefreshTokens = new RefreshTokenRepository(this, connectionString);
    }

    public async Task<IDbTransaction> BeginTransactionAsync()
    {
        if (_connection == null)
        {
            var sqlConn = new SqlConnection(_connectionString);
            await sqlConn.OpenAsync();
            _connection = sqlConn;
        }
        var tx = _connection.BeginTransaction();
        SharedTransaction = tx;
        return tx;
    }

    public async Task CommitAsync()
    {
        SharedTransaction?.Commit();
        await Task.CompletedTask;
    }

    public async Task RollbackAsync()
    {
        SharedTransaction?.Rollback();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        SharedTransaction?.Dispose();
        _connection?.Dispose();
    }
}
