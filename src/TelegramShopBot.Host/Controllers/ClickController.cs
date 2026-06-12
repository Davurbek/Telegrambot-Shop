using Microsoft.AspNetCore.Mvc;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/click")]
public class ClickController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;

    public ClickController(
        IPaymentService paymentService,
        IOrderService orderService,
        INotificationService notificationService)
    {
        _paymentService = paymentService;
        _orderService = orderService;
        _notificationService = notificationService;
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromForm] ClickCallbackRequest request)
    {
        if (!_paymentService.ValidatePaymentSignature(
            request.TransactionId, request.Amount, request.Signature))
        {
            return Ok(new { error = -1, error_note = "Invalid signature" });
        }

        try
        {
            var order = await _orderService.GetOrderByNumberAsync(request.OrderNumber);
            if (order == null)
                return Ok(new { error = -1, error_note = "Order not found" });

            await _paymentService.ProcessPaymentConfirmationAsync(
                request.OrderNumber, request.TransactionId);

            await _notificationService.NotifyPaymentSuccessAsync(
                request.OrderNumber, order.CustomerId);

            await _notificationService.NotifyAllAdminsAsync(
                $"💰 Payment received for Order {request.OrderNumber}");

            return Ok(new { error = 0, error_note = "Success" });
        }
        catch (Exception ex)
        {
            return Ok(new { error = -1, error_note = ex.Message });
        }
    }
}

public class ClickCallbackRequest
{
    [FromForm(Name = "transaction_id")]
    public string TransactionId { get; set; } = "";

    [FromForm(Name = "transaction_param")]
    public string OrderNumber { get; set; } = "";

    [FromForm(Name = "amount")]
    public string Amount { get; set; } = "0";

    [FromForm(Name = "signature")]
    public string Signature { get; set; } = "";
}
