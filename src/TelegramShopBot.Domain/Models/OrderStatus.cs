namespace TelegramShopBot.Domain.Models;

public enum OrderStatus
{
    Pending,
    Approved,
    Rejected,
    Paid,
    InDelivery,
    Delivered,
    Cancelled
}
