using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public RefreshTokenRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<RefreshToken>("SELECT * FROM RefreshTokens WHERE Token = @Token", new { Token = token }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken token)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO RefreshTokens (Token, AdminId, CreatedAt, ExpiresAt) VALUES (@Token, @AdminId, @CreatedAt, @ExpiresAt); SELECT CAST(SCOPE_IDENTITY() AS INT);", token, transaction: _uow.SharedTransaction);
            token.Id = id;
            return token;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task RevokeAsync(int tokenId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("UPDATE RefreshTokens SET RevokedAt = @Now WHERE Id = @Id", new { Id = tokenId, Now = DateTime.UtcNow }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task RevokeAllForAdminAsync(int adminId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("UPDATE RefreshTokens SET RevokedAt = @Now WHERE AdminId = @AdminId AND RevokedAt IS NULL", new { AdminId = adminId, Now = DateTime.UtcNow }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }
}

