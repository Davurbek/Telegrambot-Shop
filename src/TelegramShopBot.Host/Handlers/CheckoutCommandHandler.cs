using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class CheckoutCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var cartService = services.GetRequiredService<ICartService>();
        var userService = services.GetRequiredService<IUserService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        if (await cartService.IsCartEmptyAsync(user.TelegramId))
        {
            await BotHelper.SendMessageAsync(chatId, "🛒 Your cart is empty. Add items before checkout.");
            return;
        }

        if (!await userService.HasPhoneNumberAsync(user.TelegramId))
        {
            CommandRouter.PendingCheckout[user.TelegramId] = true;
            var contactKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("📱 Share Contact") { RequestContact = true }
            })
            { ResizeKeyboard = true, OneTimeKeyboard = true };
            await BotHelper.SendMessageAsync(chatId,
                "📞 Please share your phone number via the button below to proceed with checkout.",
                contactKeyboard);
            return;
        }

        if (!await cartService.ValidateCartStockAsync(user.TelegramId))
        {
            await BotHelper.SendMessageAsync(chatId, "❌ Some items in your cart have insufficient stock. Please review your cart.");
            return;
        }

        try
        {
            var order = await orderService.CreateOrderAsync(user.TelegramId);

            var msg = $@"✅ **Order Placed Successfully!**

Order Number: {order.OrderNumber}
Total Amount: {order.TotalAmount:N0} UZS
Status: ⏳ Pending Admin Approval

We'll notify you once your order is approved.";

            await BotHelper.SendMessageAsync(chatId, msg);

            await notificationService.NotifyOrderCreatedAsync(
                order.OrderNumber, user.FullName, order.TotalAmount);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }
}
