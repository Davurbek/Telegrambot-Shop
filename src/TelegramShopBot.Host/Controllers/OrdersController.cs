using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;

    public OrdersController(IOrderService orderService, INotificationService notificationService)
    {
        _orderService = orderService;
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderListDto>>>> GetOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null, [FromQuery] long? customerId = null,
        [FromQuery] decimal? minAmount = null, [FromQuery] decimal? maxAmount = null,
        [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
    {
        OrderStatus? statusFilter = status != null ? Enum.Parse<OrderStatus>(status, true) : null;

        var (items, total) = await _orderService.GetOrdersFilteredAsync(
            page, pageSize, statusFilter, fromDate, toDate,
            customerId, minAmount, maxAmount, sortBy, sortDirection);

        var dtos = items.Select(o => new OrderListDto
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            CustomerName = o.Customer?.FullName ?? $"User {o.CustomerId}",
            TotalAmount = o.TotalAmount,
            Status = o.Status.ToString(),
            CreatedAt = o.CreatedAt
        }).ToList();

        var result = new PagedResult<OrderListDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };

        return Ok(ApiResponse<PagedResult<OrderListDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<OrderDetailDto>.ErrorResult($"Order {id} not found"));

        var dto = new OrderDetailDto
        {
            OrderId = order.OrderId,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.FullName ?? $"User {order.CustomerId}",
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            RejectionReason = order.RejectionReason,
            TrackingNumber = order.TrackingNumber,
            EstimatedDeliveryDate = order.EstimatedDeliveryDate,
            Customer = order.Customer != null ? new CustomerInfoDto
            {
                TelegramId = order.Customer.TelegramId,
                FullName = order.Customer.FullName,
                PhoneNumber = order.Customer.PhoneNumber,
                DeliveryAddress = order.Customer.DeliveryAddress
            } : null,
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                TotalPrice = i.TotalPrice
            }).ToList(),
            Payment = order.Payment != null ? new PaymentInfoDto
            {
                TransactionId = order.Payment.TransactionId,
                Amount = order.Payment.Amount,
                PaymentDate = order.Payment.PaymentDate
            } : null
        };

        return Ok(ApiResponse<OrderDetailDto>.SuccessResult(dto));
    }

    [HttpPatch("{id}/approve")]
    public async Task<ActionResult<ApiResponse<object>>> ApproveOrder(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.ErrorResult($"Order {id} not found"));

        try
        {
            await _orderService.ApproveOrderAsync(id, order.OrderNumber, 0);
            await _notificationService.NotifyOrderApprovedAsync(order.OrderNumber, order.CustomerId);
            return Ok(ApiResponse<object>.SuccessResult(null, "Order approved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
    }

    [HttpPatch("{id}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> RejectOrder(int id, [FromBody] RejectOrderRequest request)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(ApiResponse<object>.ErrorResult($"Order {id} not found"));

        try
        {
            await _orderService.RejectOrderAsync(id, order.OrderNumber, 0, request.Reason);
            await _notificationService.NotifyOrderRejectedAsync(order.OrderNumber, order.CustomerId, request.Reason);
            return Ok(ApiResponse<object>.SuccessResult(null, "Order rejected"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        Domain.Models.Order? order = null;

        try
        {
            switch (request.Status?.ToLower())
            {
                case "indelivery":
                    order = await _orderService.MarkAsShippedAsync(id, request.TrackingNumber, request.EstimatedDeliveryDate);
                    break;
                case "delivered":
                    order = await _orderService.MarkAsDeliveredAsync(id);
                    break;
                default:
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid status value"));
            }

            if (order == null)
                return NotFound(ApiResponse<object>.ErrorResult($"Order {id} not found"));

            await _notificationService.NotifyOrderStatusChangeAsync(order.OrderNumber, order.Status.ToString(), order.CustomerId);
            return Ok(ApiResponse<object>.SuccessResult(null, $"Order status updated to {request.Status}"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
    }
}

public class OrderListDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public CustomerInfoDto? Customer { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentInfoDto? Payment { get; set; }
}

public class CustomerInfoDto
{
    public long TelegramId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? DeliveryAddress { get; set; }
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentInfoDto
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class RejectOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UpdateOrderStatusRequest
{
    public string? Status { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}
