using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Host.Handlers;

public interface ICommandHandler
{
    Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services);
}
