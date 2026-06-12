using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class SearchCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var productService = services.GetRequiredService<IProductService>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        var text = update.Message?.Text ?? "";
        var query = text.Replace("/search", "").Trim();

        if (string.IsNullOrEmpty(query))
        {
            await BotHelper.SendMessageAsync(chatId, "🔍 Send /search followed by your query.\nExample: /search iPhone");
            return;
        }

        var products = await productService.SearchProductsAsync(query);
        if (!products.Any())
        {
            await BotHelper.SendMessageAsync(chatId, $"🔍 No products found matching \"{query}\".");
            return;
        }

        var lines = new List<string> { $"🔍 **Search Results for \"{query}\"**\n" };
        foreach (var p in products)
        {
            var status = p.IsAvailable ? "✅" : "❌";
            lines.Add($"{status} **{p.Name}** - {p.Price:N0} UZS (/product_{p.ProductId})");
        }

        await BotHelper.SendMessageAsync(chatId, string.Join("\n", lines));
    }
}
