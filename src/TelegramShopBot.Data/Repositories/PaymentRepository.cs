using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public PaymentRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<Payment>("SELECT * FROM Payments WHERE OrderId = @Id", new { Id = orderId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<Payment>("SELECT * FROM Payments WHERE TransactionId = @Id", new { Id = transactionId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO Payments (OrderId, TransactionId, Amount, PaymentDate, ClickPaymentLink) VALUES (@OrderId, @TransactionId, @Amount, @PaymentDate, @ClickPaymentLink); SELECT CAST(SCOPE_IDENTITY() AS INT);", payment, transaction: _uow.SharedTransaction);
            payment.PaymentId = id;
            return payment;
        }
        finally { if (ownConn) conn.Dispose(); }
    }
}

