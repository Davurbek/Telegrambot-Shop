using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class CatalogCommandHandler : ICommandHandler
{
    private static readonly Dictionary<long, (int CategoryId, int Page)> _pageState = new();

    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var productService = services.GetRequiredService<IProductService>();
        var msgFormatter = services.GetRequiredService<Formatters.IMessageFormatter>();
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id ?? user.TelegramId;

        var categories = await productService.GetAllCategoriesAsync();
        var text = msgFormatter.FormatCategoryList(categories);

        var buttons = categories.Select(c =>
            InlineKeyboardButton.WithCallbackData(c.Name, $"cat_{c.CategoryId}")
        ).ToList();

        var keyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }));
        await BotHelper.SendMessageAsync(chatId, text, keyboard);
    }
}
