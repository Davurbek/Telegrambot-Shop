using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public AuditLogRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task CreateAsync(AuditLog log)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync(@"INSERT INTO AuditLogs (Timestamp, UserId, Action, Details, ErrorMessage) VALUES (@Timestamp, @UserId, @Action, @Details, @ErrorMessage)", log, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<AuditLog>> GetByUserIdAsync(long userId, int limit = 50)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { var result = await conn.QueryAsync<AuditLog>("SELECT * FROM AuditLogs WHERE UserId = @Id ORDER BY Timestamp DESC OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY", new { Id = userId, Limit = limit }); return result.ToList(); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<AuditLog>> GetRecentAsync(int limit = 100)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { var result = await conn.QueryAsync<AuditLog>("SELECT * FROM AuditLogs ORDER BY Timestamp DESC OFFSET 0 ROWS FETCH NEXT @Limit ROWS ONLY", new { Limit = limit }); return result.ToList(); }
        finally { if (ownConn) conn.Dispose(); }
    }
}

