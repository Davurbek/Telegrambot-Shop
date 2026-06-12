using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class CommandRouter
{
    public static readonly ConcurrentDictionary<long, bool> PendingCheckout = new();

    private readonly IServiceProvider _services;
    private readonly IUserService _userService;

    public CommandRouter(IServiceProvider services, IUserService userService)
    {
        _services = services;
        _userService = userService;
    }

    public async Task RouteAsync(Update update, UserProfile user)
    {
        if (update.Message?.Contact != null && PendingCheckout.TryRemove(user.TelegramId, out _))
        {
            var phone = update.Message.Contact.PhoneNumber;
            if (!phone.StartsWith("+")) phone = "+" + phone;
            await _userService.UpdatePhoneNumberAsync(user.TelegramId, phone);

            var removeKeyboard = new ReplyKeyboardRemove();
            await BotHelper.SendMessageAsync(user.TelegramId, "✅ Phone number saved! Proceeding with checkout...", removeKeyboard);

            var checkoutHandler = new CheckoutCommandHandler();
            await checkoutHandler.ExecuteAsync(update, user, _services);
            return;
        }

        var text = update.Message?.Text ?? update.CallbackQuery?.Data ?? "";

        if (update.CallbackQuery != null)
        {
            await HandleCallbackQuery(update, user);
            return;
        }

        ICommandHandler? handler = text switch
        {
            string s when s == "/start" => new StartCommandHandler(),
            string s when s == "/catalog" => new CatalogCommandHandler(),
            string s when s == "/cart" => new CartCommandHandler(),
            string s when s == "/checkout" => new CheckoutCommandHandler(),
            string s when s == "/orders" || s.StartsWith("/orders ") => new OrdersCommandHandler(),
            string s when s == "/profile" || s.StartsWith("/setphone ") || s.StartsWith("/setaddress ") => new ProfileCommandHandler(),
            string s when s.StartsWith("/cancel ") => new CancelOrderCommandHandler(),
            string s when s.StartsWith("/search") => new SearchCommandHandler(),
            string s when s == "/admin" || s.StartsWith("/addproduct") || s.StartsWith("/addcategory") ||
                          s.StartsWith("/approve") || s.StartsWith("/reject") || s.StartsWith("/ship") ||
                          s.StartsWith("/deliver") || s.StartsWith("/stats") || s.StartsWith("/pending") => new AdminCommandHandler(),
            _ => null
        };

        if (handler != null)
        {
            await handler.ExecuteAsync(update, user, _services);
        }
        else
        {
            if (!string.IsNullOrEmpty(text))
                await BotHelper.SendMessageAsync(user.TelegramId,
                    "❌ Unknown command. Use /start to see available options.");
        }
    }

    private async Task HandleCallbackQuery(Update update, UserProfile user)
    {
        var data = update.CallbackQuery?.Data ?? "";
        var chatId = update.CallbackQuery?.Message?.Chat.Id ?? user.TelegramId;

        if (data == "view_cart")
        {
            var cartHandler = new CartCommandHandler();
            await cartHandler.ExecuteAsync(update, user, _services);
            return;
        }

        if (data == "checkout")
        {
            var checkoutHandler = new CheckoutCommandHandler();
            await checkoutHandler.ExecuteAsync(update, user, _services);
            return;
        }

        if (data == "cart_clear")
        {
            var cartService = _services.GetRequiredService<ICartService>();
            await cartService.ClearCartAsync(user.TelegramId);
            await BotHelper.SendMessageAsync(chatId, "🛒 Savat tozalandi.");
            return;
        }

        if (data == "catalog")
        {
            var catalogHandler = new CatalogCommandHandler();
            await catalogHandler.ExecuteAsync(update, user, _services);
            return;
        }

        if (data.StartsWith("cat_"))
        {
            var parts = data.Split('_');
            var catId = int.Parse(parts[1]);
            var page = parts.Length > 2 ? int.Parse(parts[2]) : 1;
            await ShowCategoryProducts(chatId, catId, page, user);
            return;
        }

        if (data.StartsWith("prod_"))
        {
            var prodId = int.Parse(data.Replace("prod_", ""));
            var productService = _services.GetRequiredService<IProductService>();
            var msgFormatter = _services.GetRequiredService<Host.Formatters.IMessageFormatter>();

            var product = await productService.GetProductByIdAsync(prodId);
            if (product == null)
            {
                await BotHelper.SendMessageAsync(chatId, "❌ Mahsulot topilmadi.");
                return;
            }

            var text = msgFormatter.FormatProductDetails(product);
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🛒 Savatga qo'shish", $"addtocart_{product.ProductId}") },
                new[] { InlineKeyboardButton.WithCallbackData("◀️ Orqaga", $"cat_{product.CategoryId}") },
            });

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                try
                {
                    var imageUrl = ResolveImageUrl(product.ImageUrl, _services);
                    await BotHelper.SendPhotoAsync(chatId, imageUrl, text, keyboard);
                    return;
                }
                catch { }
            }

            await BotHelper.SendMessageAsync(chatId, text, keyboard);
            return;
        }

        if (data.StartsWith("addtocart_"))
        {
            try
            {
                var prodId = int.Parse(data.Replace("addtocart_", ""));
                var cartService = _services.GetRequiredService<ICartService>();
                var productService = _services.GetRequiredService<IProductService>();

                var product = await productService.GetProductByIdAsync(prodId);
                if (product == null || !product.IsAvailable)
                {
                    await BotHelper.AnswerCallbackAsync(update);
                    await BotHelper.SendMessageAsync(chatId, "❌ Mahsulot mavjud emas yoki tugagan.");
                    return;
                }

                await cartService.AddToCartAsync(user.TelegramId, prodId, 1);
                var cartItems = await cartService.GetCartItemsAsync(user.TelegramId);
                var cartTotal = await cartService.GetCartTotalAsync(user.TelegramId);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("🛒 Savatni ko'rish", "view_cart") },
                    new[] { InlineKeyboardButton.WithCallbackData("◀️ Davom etish", $"cat_{product.CategoryId}") },
                });

                await BotHelper.AnswerCallbackAsync(update, $"✅ {product.Name} savatga qo'shildi!");
                await BotHelper.SendMessageAsync(chatId,
                    $"✅ *{product.Name}* savatga qo'shildi!\n\n🛒 Savatda: {cartItems.Count} ta mahsulot\n💰 Jami: {cartTotal:N0} UZS",
                    keyboard);
            }
            catch (Exception ex)
            {
                await BotHelper.AnswerCallbackAsync(update, "❌ Xatolik yuz berdi");
                await BotHelper.SendMessageAsync(chatId, $"❌ Xatolik: {ex.Message}");
            }
            return;
        }

        if (data.StartsWith("order_"))
        {
            var orderId = int.Parse(data.Replace("order_", ""));
            var orderService = _services.GetRequiredService<IOrderService>();
            var msgFormatter = _services.GetRequiredService<Host.Formatters.IMessageFormatter>();

            var order = await orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                var text = msgFormatter.FormatOrderDetails(order);
                await BotHelper.SendMessageAsync(chatId, text);
            }
            return;
        }

        await BotHelper.SendMessageAsync(chatId, "❌ Noma'lum amal.");
    }

    private static string ResolveImageUrl(string? imageUrl, IServiceProvider services)
    {
        if (string.IsNullOrEmpty(imageUrl)) return "";
        if (imageUrl.StartsWith("http")) return imageUrl;
        var configuration = services.GetRequiredService<IConfiguration>();
        var baseUrl = configuration["BaseUrl"]?.TrimEnd('/') ?? "https://localhost:44337";
        return $"{baseUrl}{imageUrl}";
    }

    private async Task ShowCategoryProducts(long chatId, int catId, int page, UserProfile user)
    {
        var productService = _services.GetRequiredService<IProductService>();
        var msgFormatter = _services.GetRequiredService<Host.Formatters.IMessageFormatter>();

        var pageSize = 10;
        var (products, totalCount) = await productService.GetProductsByCategoryAsync(catId, page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var text = msgFormatter.FormatProductList(products, page, totalPages);
        var buttons = new List<List<InlineKeyboardButton>>();

        foreach (var p in products)
        {
            var status = p.IsAvailable ? "✅" : "❌";
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"{status} {p.Name} - {p.Price:N0} UZS", $"prod_{p.ProductId}")
            });
        }

        var navRow = new List<InlineKeyboardButton>();
        if (page > 1)
            navRow.Add(InlineKeyboardButton.WithCallbackData("⬅️ Oldingi", $"cat_{catId}_{page - 1}"));
        if (page < totalPages)
            navRow.Add(InlineKeyboardButton.WithCallbackData("Keyingi ➡️", $"cat_{catId}_{page + 1}"));

        if (navRow.Count > 0)
            buttons.Add(navRow);

        buttons.Add(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("🏠 Bosh menyu", "catalog") });

        var keyboard = new InlineKeyboardMarkup(buttons);
        await BotHelper.SendMessageAsync(chatId, text, keyboard);
    }
}
