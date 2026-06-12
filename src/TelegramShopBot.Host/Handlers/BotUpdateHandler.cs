using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramShopBot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TelegramShopBot.Host.Handlers;

public class BotUpdateHandler
{
    private readonly IServiceProvider _services;
    private readonly IBotLogger _logger;

    public BotUpdateHandler(IServiceProvider services)
    {
        _services = services;
        _logger = services.GetRequiredService<IBotLogger>();
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            if (update.Type != UpdateType.Message && update.Type != UpdateType.CallbackQuery)
                return;

            User? telegramUser = null;
            if (update.Message?.From != null)
                telegramUser = update.Message.From;
            else if (update.CallbackQuery?.From != null)
                telegramUser = update.CallbackQuery.From;

            if (telegramUser == null) return;

            using (var scope = _services.CreateScope())
            {
                var sp = scope.ServiceProvider;
                var userService = sp.GetRequiredService<IUserService>();

                var user = await userService.RegisterOrGetUserAsync(
                    telegramUser.Id,
                    telegramUser.Username,
                    telegramUser.FirstName,
                    telegramUser.LastName
                );

                var router = new CommandRouter(sp, userService);
                await router.RouteAsync(update, user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update");

            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
            if (chatId.HasValue)
            {
                try
                {
                    await botClient.SendMessage(chatId.Value,
                        "❌ An error occurred. Please try again later.");
                }
                catch { }
            }
        }
    }
}
