using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class CartCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var cartService = services.GetRequiredService<ICartService>();
        var msgFormatter = services.GetRequiredService<Formatters.IMessageFormatter>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        var items = await cartService.GetCartItemsAsync(user.TelegramId);
        var total = await cartService.GetCartTotalAsync(user.TelegramId);
        var text = msgFormatter.FormatCart(items, total);

        if (items.Any())
        {
            await BotHelper.SendMessageAsync(chatId, text,
                BotHelper.CreateInlineKeyboard(
                    ("/checkout - Place Order", "checkout"),
                    ("/cart clear - Clear Cart", "cart_clear")
                ));
        }
        else
        {
            await BotHelper.SendMessageAsync(chatId, text);
        }
    }
}
