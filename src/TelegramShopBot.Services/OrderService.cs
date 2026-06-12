using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly ICartService _cartService;

    public OrderService(IUnitOfWork uow, ICartService cartService)
    {
        _uow = uow;
        _cartService = cartService;
    }

    public async Task<Order> CreateOrderAsync(long customerId)
    {
        var cartItems = await _cartService.GetCartItemsAsync(customerId);
        if (!cartItems.Any()) throw new InvalidOperationException("Cart is empty");

        foreach (var item in cartItems)
        {
            if (item.Product == null || item.Quantity > item.Product.StockQuantity)
                throw new InvalidOperationException($"Insufficient stock for {item.Product?.Name ?? "product"}");
        }

        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");
        var sequence = await _uow.Orders.GetNextSequenceForDateAsync(datePrefix);
        var orderNumber = $"ORD-{datePrefix}-{sequence:D5}";

        var orderItems = cartItems.Select(ci => new OrderItem
        {
            ProductId = ci.ProductId,
            ProductName = ci.Product?.Name ?? "",
            UnitPrice = ci.Product?.Price ?? 0,
            Quantity = ci.Quantity
        }).ToList();

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            TotalAmount = orderItems.Sum(i => i.TotalPrice),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        await _uow.BeginTransactionAsync();
        try
        {
            order = await _uow.Orders.CreateAsync(order);

            foreach (var item in cartItems)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId);
                if (product == null)
                    throw new InvalidOperationException($"Product (ID: {item.ProductId}) not found");

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}, requested: {item.Quantity}");

                product.StockQuantity -= item.Quantity;
                await _uow.Products.UpdateAsync(product);
            }

            await _cartService.ClearCartAsync(customerId);
            await _uow.CommitAsync();
        }
        catch
        {
            await _uow.RollbackAsync();
            throw;
        }
        finally
        {
            _uow.SharedTransaction = null;
        }

        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId) =>
        await _uow.Orders.GetByIdAsync(orderId);

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber) =>
        await _uow.Orders.GetByOrderNumberAsync(orderNumber);

    public async Task<List<Order>> GetOrderHistoryAsync(long customerId) =>
        await _uow.Orders.GetByCustomerAsync(customerId);

    public async Task<List<Order>> GetOrdersByStatusAsync(OrderStatus status) =>
        await _uow.Orders.GetByStatusAsync(status);

    public async Task ApproveOrderAsync(int orderId, string orderNumber, long adminId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) throw new InvalidOperationException("Order not found");
        if (order.Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be approved");

        order.Status = OrderStatus.Approved;
        order.UpdatedAt = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(order);
    }

    public async Task RejectOrderAsync(int orderId, string orderNumber, long adminId, string reason)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) throw new InvalidOperationException("Order not found");
        if (order.Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be rejected");

        await _uow.BeginTransactionAsync();
        try
        {
            order.Status = OrderStatus.Rejected;
            order.RejectionReason = reason;
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.Orders.UpdateAsync(order);

            foreach (var item in order.OrderItems)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    await _uow.Products.UpdateAsync(product);
                }
            }

            await _uow.CommitAsync();
        }
        catch
        {
            await _uow.RollbackAsync();
            throw;
        }
        finally
        {
            _uow.SharedTransaction = null;
        }
    }

    public async Task CancelOrderAsync(int orderId, long customerId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null || order.CustomerId != customerId)
            throw new InvalidOperationException("Order not found");

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Approved)
            throw new InvalidOperationException("Order cannot be cancelled in its current status");

        await _uow.BeginTransactionAsync();
        try
        {
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _uow.Orders.UpdateAsync(order);

            foreach (var item in order.OrderItems)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    await _uow.Products.UpdateAsync(product);
                }
            }

            await _uow.CommitAsync();
        }
        catch
        {
            await _uow.RollbackAsync();
            throw;
        }
        finally
        {
            _uow.SharedTransaction = null;
        }
    }

    public async Task<Order?> MarkAsShippedAsync(int orderId, string? trackingNumber, DateTime? estimatedDelivery)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return null;
        if (order.Status != OrderStatus.Paid && order.Status != OrderStatus.Approved)
            throw new InvalidOperationException("Order must be approved or paid before shipping");

        order.Status = OrderStatus.InDelivery;
        order.TrackingNumber = trackingNumber;
        order.EstimatedDeliveryDate = estimatedDelivery;
        order.UpdatedAt = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(order);
        return order;
    }

    public async Task<Order?> MarkAsDeliveredAsync(int orderId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return null;
        if (order.Status != OrderStatus.InDelivery) throw new InvalidOperationException("Only shipped orders can be delivered");

        order.Status = OrderStatus.Delivered;
        order.UpdatedAt = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(order);
        return order;
    }

    public async Task<OrderStatistics> GetStatisticsAsync()
    {
        var stats = new OrderStatistics();
        var allOrders = await _uow.Orders.GetAllAsync(1, int.MaxValue);

        stats.TotalOrders = allOrders.Count;
        stats.TotalRevenue = allOrders
            .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.InDelivery || o.Status == OrderStatus.Delivered)
            .Sum(o => o.TotalAmount);

        stats.OrdersByStatus = allOrders
            .GroupBy(o => o.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    public async Task<(List<Order> Items, int TotalCount)> GetOrdersFilteredAsync(
        int page, int pageSize, OrderStatus? status, DateTime? fromDate,
        DateTime? toDate, long? customerId, decimal? minAmount, decimal? maxAmount,
        string? sortBy, string? sortDirection)
    {
        var items = await _uow.Orders.GetFilteredAsync(page, pageSize, status, fromDate, toDate,
            customerId, minAmount, maxAmount, sortBy, sortDirection);
        var total = await _uow.Orders.GetFilteredCountAsync(status, fromDate, toDate,
            customerId, minAmount, maxAmount);
        return (items, total);
    }

    public async Task<List<(string ProductName, int TotalQuantitySold, decimal TotalRevenue)>> GetTopSellingProductsAsync(int count = 5)
    {
        var allOrders = await _uow.Orders.GetAllAsync(1, int.MaxValue);
        var paidStatuses = new[] { OrderStatus.Paid, OrderStatus.InDelivery, OrderStatus.Delivered };

        var topProducts = allOrders
            .Where(o => paidStatuses.Contains(o.Status))
            .SelectMany(o =>
            {
                if (o.OrderItems.Any()) return o.OrderItems;
                return Enumerable.Empty<OrderItem>();
            })
            .GroupBy(i => i.ProductName)
            .Select(g => (
                ProductName: g.Key,
                TotalQuantitySold: g.Sum(i => i.Quantity),
                TotalRevenue: g.Sum(i => i.TotalPrice)))
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToList();

        return topProducts;
    }
}
