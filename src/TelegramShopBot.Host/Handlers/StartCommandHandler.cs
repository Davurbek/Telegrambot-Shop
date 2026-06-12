using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class StartCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var msgFormatter = services.GetRequiredService<Formatters.IMessageFormatter>();
        var userService = services.GetRequiredService<IUserService>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        var text = msgFormatter.FormatWelcomeMessage(user);
        var isAdmin = await userService.IsAdminAsync(user.TelegramId);

        await BotHelper.SendMessageAsync(chatId, text,
            isAdmin ? BotHelper.CreateAdminMenu() : BotHelper.CreateMainMenu());
    }
}
