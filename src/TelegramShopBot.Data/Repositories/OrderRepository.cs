using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;
using System.Text;

namespace TelegramShopBot.Data.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public OrderRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var order = await conn.QueryFirstOrDefaultAsync<Order>("SELECT * FROM Orders WHERE OrderId = @Id", new { Id = orderId });
            if (order != null)
            {
                order.OrderItems = (await conn.QueryAsync<OrderItem>("SELECT * FROM OrderItems WHERE OrderId = @Id", new { Id = orderId })).ToList();
                order.Customer = await conn.QueryFirstOrDefaultAsync<UserProfile>("SELECT * FROM Users WHERE TelegramId = @Id", new { Id = order.CustomerId });
                order.Payment = await conn.QueryFirstOrDefaultAsync<Payment>("SELECT * FROM Payments WHERE OrderId = @Id", new { Id = orderId });
            }
            return order;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var order = await conn.QueryFirstOrDefaultAsync<Order>("SELECT * FROM Orders WHERE OrderNumber = @Num", new { Num = orderNumber });
            if (order != null)
            {
                order.OrderItems = (await conn.QueryAsync<OrderItem>("SELECT * FROM OrderItems WHERE OrderId = @Id", new { Id = order.OrderId })).ToList();
            }
            return order;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Order>> GetByCustomerAsync(long customerId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var result = await conn.QueryAsync<Order>("SELECT * FROM Orders WHERE CustomerId = @CustId ORDER BY CreatedAt DESC", new { CustId = customerId });
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Order>> GetByStatusAsync(OrderStatus status)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var result = await conn.QueryAsync<Order>("SELECT * FROM Orders WHERE Status = @Status ORDER BY CreatedAt DESC", new { Status = status.ToString() });
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Order>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var offset = (page - 1) * pageSize;
            var result = await conn.QueryAsync<Order>("SELECT * FROM Orders ORDER BY CreatedAt DESC OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY", new { Limit = pageSize, Offset = offset });
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetTotalCountAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders"); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<Order> CreateAsync(Order order)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var id = await conn.ExecuteScalarAsync<int>(@"INSERT INTO Orders (OrderNumber, CustomerId, TotalAmount, Status, CreatedAt) VALUES (@OrderNumber, @CustomerId, @TotalAmount, @Status, @CreatedAt); SELECT CAST(SCOPE_IDENTITY() AS INT);", new { order.OrderNumber, order.CustomerId, order.TotalAmount, Status = order.Status.ToString(), order.CreatedAt }, transaction: _uow.SharedTransaction);
            order.OrderId = id;
            foreach (var item in order.OrderItems)
            {
                item.OrderId = id;
                await conn.ExecuteAsync(@"INSERT INTO OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity) VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity)", item, transaction: _uow.SharedTransaction);
            }
            return order;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task UpdateAsync(Order order)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            await conn.ExecuteAsync(@"UPDATE Orders SET Status=@Status, UpdatedAt=@UpdatedAt, RejectionReason=@RejectionReason, TrackingNumber=@TrackingNumber, EstimatedDeliveryDate=@EstimatedDeliveryDate WHERE OrderId=@OrderId", new { Status = order.Status.ToString(), order.UpdatedAt, order.RejectionReason, order.TrackingNumber, order.EstimatedDeliveryDate, order.OrderId }, transaction: _uow.SharedTransaction);
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetNextSequenceForDateAsync(string datePrefix)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var max = await conn.ExecuteScalarAsync<string>("SELECT MAX(OrderNumber) FROM Orders WHERE OrderNumber LIKE @Prefix", new { Prefix = $"ORD-{datePrefix}-%" });
            if (max == null) return 1;
            var parts = max.Split('-');
            return int.Parse(parts[2]) + 1;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<Order>> GetFilteredAsync(int page, int pageSize, OrderStatus? status, DateTime? fromDate, DateTime? toDate, long? customerId, decimal? minAmount, decimal? maxAmount, string? sortBy, string? sortDirection)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var sql = new StringBuilder("SELECT * FROM Orders WHERE 1=1");
            var parameters = new DynamicParameters();
            if (status.HasValue) { sql.Append(" AND Status = @Status"); parameters.Add("Status", status.Value.ToString()); }
            if (fromDate.HasValue) { sql.Append(" AND CreatedAt >= @From"); parameters.Add("From", fromDate.Value.ToString("O")); }
            if (toDate.HasValue) { sql.Append(" AND CreatedAt <= @To"); parameters.Add("To", toDate.Value.ToString("O")); }
            if (customerId.HasValue) { sql.Append(" AND CustomerId = @CustId"); parameters.Add("CustId", customerId.Value); }
            if (minAmount.HasValue) { sql.Append(" AND TotalAmount >= @MinAmt"); parameters.Add("MinAmt", minAmount.Value); }
            if (maxAmount.HasValue) { sql.Append(" AND TotalAmount <= @MaxAmt"); parameters.Add("MaxAmt", maxAmount.Value); }
            var sort = (sortBy ?? "CreatedAt") switch { "totalAmount" => "TotalAmount", "status" => "Status", _ => "CreatedAt" };
            var dir = (sortDirection ?? "DESC").ToUpper() == "ASC" ? "ASC" : "DESC";
            sql.Append($" ORDER BY {sort} {dir}");
            var offset = (page - 1) * pageSize;
            sql.Append(" OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY");
            parameters.Add("Limit", pageSize);
            parameters.Add("Offset", offset);
            var result = await conn.QueryAsync<Order>(sql.ToString(), parameters);
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<int> GetFilteredCountAsync(OrderStatus? status, DateTime? fromDate, DateTime? toDate, long? customerId, decimal? minAmount, decimal? maxAmount)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var sql = new StringBuilder("SELECT COUNT(*) FROM Orders WHERE 1=1");
            var parameters = new DynamicParameters();
            if (status.HasValue) { sql.Append(" AND Status = @Status"); parameters.Add("Status", status.Value.ToString()); }
            if (fromDate.HasValue) { sql.Append(" AND CreatedAt >= @From"); parameters.Add("From", fromDate.Value.ToString("O")); }
            if (toDate.HasValue) { sql.Append(" AND CreatedAt <= @To"); parameters.Add("To", toDate.Value.ToString("O")); }
            if (customerId.HasValue) { sql.Append(" AND CustomerId = @CustId"); parameters.Add("CustId", customerId.Value); }
            if (minAmount.HasValue) { sql.Append(" AND TotalAmount >= @MinAmt"); parameters.Add("MinAmt", minAmount.Value); }
            if (maxAmount.HasValue) { sql.Append(" AND TotalAmount <= @MaxAmt"); parameters.Add("MaxAmt", maxAmount.Value); }
            return await conn.ExecuteScalarAsync<int>(sql.ToString(), parameters);
        }
        finally { if (ownConn) conn.Dispose(); }
    }
}

