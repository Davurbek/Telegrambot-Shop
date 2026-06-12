namespace TelegramShopBot.Domain.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ClickPaymentLink { get; set; }

    public Order? Order { get; set; }
}
