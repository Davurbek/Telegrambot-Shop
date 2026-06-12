using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public CategoryRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<Category?> GetByIdAsync(int categoryId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<Category>("SELECT * FROM Categories WHERE CategoryId = @Id", new { Id = categoryId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Category>> GetAllAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { var result = await conn.QueryAsync<Category>("SELECT * FROM Categories ORDER BY Name"); return result.ToList(); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Category> CreateAsync(Category category)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO Categories (Name, Description) VALUES (@Name, @Description); SELECT CAST(SCOPE_IDENTITY() AS INT);", category, transaction: _uow.SharedTransaction);
            category.CategoryId = id;
            return category;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("UPDATE Categories SET Name=@Name, Description=@Description WHERE CategoryId=@CategoryId", category, transaction: _uow.SharedTransaction); return category; }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task DeleteAsync(int categoryId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("DELETE FROM Categories WHERE CategoryId = @Id", new { Id = categoryId }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetProductCountAsync(int categoryId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products WHERE CategoryId = @Id", new { Id = categoryId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Category>> GetPagedAsync(int page, int pageSize)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var offset = (page - 1) * pageSize;
            var result = await conn.QueryAsync<Category>("SELECT * FROM Categories ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY", new { Limit = pageSize, Offset = offset });
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetTotalCountAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Categories"); }
        finally { if (ownConn) conn.Dispose(); }
    }
}

