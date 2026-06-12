using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public ProductRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<Product?> GetByIdAsync(int productId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<Product>("SELECT * FROM Products WHERE ProductId = @Id", new { Id = productId }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Product>> GetByCategoryAsync(int categoryId, int page = 1, int pageSize = 10)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var offset = (page - 1) * pageSize;
            var result = await conn.QueryAsync<Product>("SELECT * FROM Products WHERE CategoryId = @CatId ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY", new { CatId = categoryId, Limit = pageSize, Offset = offset });
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetCountByCategoryAsync(int categoryId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products WHERE CategoryId = @Id", new { Id = categoryId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Product>> SearchAsync(string query)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var result = await conn.QueryAsync<Product>("SELECT * FROM Products WHERE Name LIKE @Q OR Description LIKE @Q ORDER BY Name", new { Q = $"%{query}%" });
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Product>> GetAllAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var result = await conn.QueryAsync<Product>("SELECT p.*, c.Name as CategoryName FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.CategoryId ORDER BY p.Name");
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Product> CreateAsync(Product product)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO Products (Name, Description, Price, CategoryId, ImageUrl, StockQuantity) VALUES (@Name, @Description, @Price, @CategoryId, @ImageUrl, @StockQuantity); SELECT CAST(SCOPE_IDENTITY() AS INT);", product, transaction: _uow.SharedTransaction);
            product.ProductId = id;
            return product;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Product> UpdateAsync(Product product)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            await conn.ExecuteAsync(@"UPDATE Products SET Name=@Name, Description=@Description, Price=@Price, CategoryId=@CategoryId, ImageUrl=@ImageUrl, StockQuantity=@StockQuantity WHERE ProductId=@ProductId", product, transaction: _uow.SharedTransaction);
            return product;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task DeleteAsync(int productId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("DELETE FROM Products WHERE ProductId = @Id", new { Id = productId }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetTotalCountAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products"); }
        finally { if (ownConn) conn.Dispose(); }
    }
}

