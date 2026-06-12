namespace TelegramShopBot.Services;

public interface IPaymentService
{
    Task<string> GeneratePaymentLinkAsync(string orderNumber, decimal amount, long customerId);
    bool ValidatePaymentSignature(string transactionId, string amount, string signature);
    Task ProcessPaymentConfirmationAsync(string orderNumber, string transactionId);
}
