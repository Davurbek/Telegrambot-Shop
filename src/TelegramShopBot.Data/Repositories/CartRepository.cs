using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class CartRepository : ICartRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public CartRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    private async Task<SqlConnection> GetConnectionAsync()
    {
        if (_uow.SharedConnection != null) return null!;
        var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        return conn;
    }

    public async Task<CartItem?> GetByCustomerAndProductAsync(long customerId, int productId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<CartItem>("SELECT * FROM CartItems WHERE CustomerId = @CustId AND ProductId = @ProdId", new { CustId = customerId, ProdId = productId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<CartItem>> GetByCustomerAsync(long customerId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var items = (await conn.QueryAsync<CartItem>("SELECT * FROM CartItems WHERE CustomerId = @CustId", new { CustId = customerId })).ToList();
            if (items.Count > 0)
            {
                var productIds = items.Select(i => i.ProductId).Distinct();
                var products = (await conn.QueryAsync<Product>("SELECT * FROM Products WHERE ProductId IN @Ids", new { Ids = productIds })).ToDictionary(p => p.ProductId);
                foreach (var item in items)
                    if (products.TryGetValue(item.ProductId, out var product))
                        item.Product = product;
            }
            return items;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<CartItem> CreateAsync(CartItem item)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO CartItems (CustomerId, ProductId, Quantity) VALUES (@CustomerId, @ProductId, @Quantity); SELECT CAST(SCOPE_IDENTITY() AS INT);", item, transaction: _uow.SharedTransaction);
            item.CartItemId = id;
            return item;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task UpdateAsync(CartItem item)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("UPDATE CartItems SET Quantity = @Quantity WHERE CartItemId = @CartItemId", item, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task DeleteAsync(int cartItemId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("DELETE FROM CartItems WHERE CartItemId = @Id", new { Id = cartItemId }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task ClearByCustomerAsync(long customerId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { await conn.ExecuteAsync("DELETE FROM CartItems WHERE CustomerId = @CustId", new { CustId = customerId }, transaction: _uow.SharedTransaction); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetCountByCustomerAsync(long customerId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM CartItems WHERE CustomerId = @CustId", new { CustId = customerId }); }
        finally { if (ownConn) conn.Dispose(); }
    }
}

