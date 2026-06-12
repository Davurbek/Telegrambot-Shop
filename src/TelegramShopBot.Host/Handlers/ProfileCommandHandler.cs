using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class ProfileCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var userService = services.GetRequiredService<IUserService>();
        var msgFormatter = services.GetRequiredService<Formatters.IMessageFormatter>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        var text = update.Message?.Text ?? "";

        if (text.StartsWith("/setphone"))
        {
            var phone = text.Replace("/setphone", "").Trim();
            if (string.IsNullOrEmpty(phone))
            {
                await BotHelper.SendMessageAsync(chatId, "Usage: /setphone +998901234567");
                return;
            }
            try
            {
                await userService.UpdatePhoneNumberAsync(user.TelegramId, phone);
                await BotHelper.SendMessageAsync(chatId, "✅ Phone number updated!");
            }
            catch (Exception ex)
            {
                await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
            }
            return;
        }

        if (text.StartsWith("/setaddress"))
        {
            var address = text.Replace("/setaddress", "").Trim();
            if (string.IsNullOrEmpty(address))
            {
                await BotHelper.SendMessageAsync(chatId, "Usage: /setaddress Tashkent, Chilonzor, 12");
                return;
            }
            try
            {
                await userService.UpdateDeliveryAddressAsync(user.TelegramId, address);
                await BotHelper.SendMessageAsync(chatId, "✅ Delivery address updated!");
            }
            catch (Exception ex)
            {
                await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
            }
            return;
        }

        var profile = await userService.GetUserProfileAsync(user.TelegramId);
        if (profile == null) return;

        var profileText = msgFormatter.FormatProfile(profile);
        await BotHelper.SendMessageAsync(chatId, profileText);
    }
}
