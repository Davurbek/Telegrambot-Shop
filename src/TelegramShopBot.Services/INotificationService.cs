namespace TelegramShopBot.Services;

public interface INotificationService
{
    Task NotifyCustomerAsync(long customerId, string message);
    Task NotifyAllAdminsAsync(string message);
    Task NotifyOrderStatusChangeAsync(string orderNumber, string status, long customerId);
    Task NotifyOrderCreatedAsync(string orderNumber, string customerName, decimal totalAmount);
    Task NotifyOrderApprovedAsync(string orderNumber, long customerId, string? paymentLink = null);
    Task NotifyOrderRejectedAsync(string orderNumber, long customerId, string reason);
    Task NotifyOrderCancelledAsync(string orderNumber, long customerId);
    Task NotifyPaymentSuccessAsync(string orderNumber, long customerId);
    Task NotifyCriticalErrorAsync(string errorMessage);
}
