using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class OrdersCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var msgFormatter = services.GetRequiredService<Formatters.IMessageFormatter>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        var text = update.Message?.Text;

        if (text?.StartsWith("/orders") == true && text.Contains(' '))
        {
            var orderNum = text.Split(' ')[1].Trim();
            var order = await orderService.GetOrderByNumberAsync(orderNum);
            if (order == null || order.CustomerId != user.TelegramId)
            {
                await BotHelper.SendMessageAsync(chatId, "❌ Order not found.");
                return;
            }
            var detailText = msgFormatter.FormatOrderDetails(order);
            await BotHelper.SendMessageAsync(chatId, detailText);
            return;
        }

        var orders = await orderService.GetOrderHistoryAsync(user.TelegramId);
        var historyText = msgFormatter.FormatOrderHistory(orders);

        if (orders.Any())
        {
            var buttons = orders.Take(5).Select(o =>
                InlineKeyboardButton.WithCallbackData(o.OrderNumber, $"order_{o.OrderId}")
            ).Select(b => new[] { b }).ToList();

            await BotHelper.SendMessageAsync(chatId, historyText,
                buttons.Any() ? new InlineKeyboardMarkup(buttons) : null);
        }
        else
        {
            await BotHelper.SendMessageAsync(chatId, historyText);
        }
    }
}
