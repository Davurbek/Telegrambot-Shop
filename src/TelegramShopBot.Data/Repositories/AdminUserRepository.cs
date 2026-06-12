using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public AdminUserRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<AdminUser?> GetByIdAsync(int id)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<AdminUser>("SELECT * FROM AdminUsers WHERE Id = @Id", new { Id = id }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<AdminUser?> GetByEmailAsync(string email)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<AdminUser>("SELECT * FROM AdminUsers WHERE Email = @Email", new { Email = email }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<AdminUser> CreateAsync(AdminUser admin)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO AdminUsers (Email, PasswordHash, FullName, Role, IsActive, CreatedAt) VALUES (@Email, @PasswordHash, @FullName, @Role, @IsActive, @CreatedAt); SELECT CAST(SCOPE_IDENTITY() AS INT);", admin, transaction: _uow.SharedTransaction);
            admin.Id = id;
            return admin;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task UpdateAsync(AdminUser admin)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync(@"UPDATE AdminUsers SET Email=@Email, PasswordHash=@PasswordHash, FullName=@FullName, Role=@Role, IsActive=@IsActive, LastLoginAt=@LastLoginAt WHERE Id=@Id", admin, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<AdminUser>> GetAllActiveAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { var result = await conn.QueryAsync<AdminUser>("SELECT * FROM AdminUsers WHERE IsActive = 1"); return result.ToList(); }
        finally { if (ownConn) conn.Dispose(); }
    }
}

