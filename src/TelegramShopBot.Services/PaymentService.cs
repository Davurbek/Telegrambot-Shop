using System.Security.Cryptography;
using System.Text;
using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly string _serviceId;
    private readonly string _merchantId;
    private readonly string _secretKey;
    private readonly string _returnUrl;

    public PaymentService(IUnitOfWork uow, string serviceId, string merchantId, string secretKey, string returnUrl)
    {
        _uow = uow;
        _serviceId = serviceId;
        _merchantId = merchantId;
        _secretKey = secretKey;
        _returnUrl = returnUrl;
    }

    public async Task<string> GeneratePaymentLinkAsync(string orderNumber, decimal amount, long customerId)
    {
        var order = await _uow.Orders.GetByOrderNumberAsync(orderNumber);
        if (order == null) throw new InvalidOperationException("Order not found");
        if (order.Status != OrderStatus.Approved) throw new InvalidOperationException("Order is not approved");

        var link = $"https://my.click.uz/services/pay?service_id={_serviceId}&merchant_id={_merchantId}&amount={amount:F2}&transaction_param={orderNumber}&return_url={_returnUrl}";

        return link;
    }

    public bool ValidatePaymentSignature(string transactionId, string amount, string signature)
    {
        var message = $"{transactionId}:{amount}:{_secretKey}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
        return signature.Equals(computed, StringComparison.OrdinalIgnoreCase);
    }

    public async Task ProcessPaymentConfirmationAsync(string orderNumber, string transactionId)
    {
        var order = await _uow.Orders.GetByOrderNumberAsync(orderNumber);
        if (order == null) throw new InvalidOperationException("Order not found");
        if (order.Status != OrderStatus.Approved) throw new InvalidOperationException("Order is not approved");

        var existingPayment = await _uow.Payments.GetByTransactionIdAsync(transactionId);
        if (existingPayment != null) throw new InvalidOperationException("Duplicate payment");

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(order);

        await _uow.Payments.CreateAsync(new Payment
        {
            OrderId = order.OrderId,
            TransactionId = transactionId,
            Amount = order.TotalAmount,
            PaymentDate = DateTime.UtcNow
        });
    }
}
