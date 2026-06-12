using Microsoft.Extensions.DependencyInjection;
using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class NotificationService : INotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private Func<long, string, Task>? _sendMessageFunc;

    public NotificationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void SetSendMessageFunc(Func<long, string, Task> func)
    {
        _sendMessageFunc = func;
    }

    public async Task NotifyCustomerAsync(long customerId, string message)
    {
        await SendWithRetryAsync(customerId, message);
    }

    public async Task NotifyAllAdminsAsync(string message)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var admins = await uow.Users.GetAdminsAsync();
            foreach (var admin in admins)
            {
                await SendWithRetryAsync(admin.TelegramId, message);
            }
        }
    }

    public async Task NotifyOrderStatusChangeAsync(string orderNumber, string status, long customerId)
    {
        var message = $"📦 Order {orderNumber}\nStatus: {status}";
        await NotifyCustomerAsync(customerId, message);
    }

    public async Task NotifyOrderCreatedAsync(string orderNumber, string customerName, decimal totalAmount)
    {
        var message = $"🔔 New Order Received\n\nOrder: {orderNumber}\nCustomer: {customerName}\nAmount: {totalAmount:N0} UZS";
        await NotifyAllAdminsAsync(message);
    }

    public async Task NotifyOrderApprovedAsync(string orderNumber, long customerId, string? paymentLink = null)
    {
        var message = $"✅ Order {orderNumber} Approved!\n\nYour order has been approved.";
        if (!string.IsNullOrEmpty(paymentLink))
            message += $"\n\n💳 Click here to pay: {paymentLink}";
        await NotifyCustomerAsync(customerId, message);
    }

    public async Task NotifyOrderRejectedAsync(string orderNumber, long customerId, string reason)
    {
        var message = $"❌ Order {orderNumber} Rejected\n\nReason: {reason}";
        await NotifyCustomerAsync(customerId, message);
    }

    public async Task NotifyOrderCancelledAsync(string orderNumber, long customerId)
    {
        var message = $"✅ Order {orderNumber} has been cancelled successfully.";
        await NotifyCustomerAsync(customerId, message);

        var cancelMsg = $"🔔 Order {orderNumber} was cancelled by the customer.";
        await NotifyAllAdminsAsync(cancelMsg);
    }

    public async Task NotifyPaymentSuccessAsync(string orderNumber, long customerId)
    {
        var message = $"✅ Payment Successful!\n\nOrder {orderNumber} has been paid. We'll notify you when it ships.";
        await NotifyCustomerAsync(customerId, message);
    }

    public async Task NotifyCriticalErrorAsync(string errorMessage)
    {
        await NotifyAllAdminsAsync($"🚨 Critical Error\n\n{errorMessage}");
    }

    private async Task SendWithRetryAsync(long chatId, string message, int maxRetries = 3)
    {
        if (_sendMessageFunc == null) return;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                await _sendMessageFunc(chatId, message);
                return;
            }
            catch
            {
                if (attempt == maxRetries - 1) throw;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
            }
        }
    }
}
