using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<Payment> CreateAsync(Payment payment);
}
