using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class CancelOrderCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        var text = update.Message?.Text ?? "";
        var parts = text.Split(' ');
        if (parts.Length < 2)
        {
            await BotHelper.SendMessageAsync(chatId, "Usage: /cancel ORD-20241220-00001");
            return;
        }

        var orderNumber = parts[1].Trim();
        var order = await orderService.GetOrderByNumberAsync(orderNumber);

        if (order == null || order.CustomerId != user.TelegramId)
        {
            await BotHelper.SendMessageAsync(chatId, "❌ Order not found.");
            return;
        }

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Approved)
        {
            await BotHelper.SendMessageAsync(chatId, "❌ This order cannot be cancelled in its current status.");
            return;
        }

        try
        {
            await orderService.CancelOrderAsync(order.OrderId, user.TelegramId);
            await BotHelper.SendMessageAsync(chatId, $"✅ Order {orderNumber} has been cancelled successfully.");
            await notificationService.NotifyOrderCancelledAsync(orderNumber, user.TelegramId);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }
}
