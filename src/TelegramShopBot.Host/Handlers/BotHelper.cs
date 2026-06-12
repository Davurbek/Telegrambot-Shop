using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramShopBot.Host.Handlers;

public static class BotHelper
{
    public static ITelegramBotClient? BotClient { get; set; }

    public static async Task SendMessageAsync(long chatId, string text, ReplyMarkup? replyMarkup = null)
    {
        if (BotClient == null) return;
        await BotClient.SendMessage(
            chatId: chatId,
            text: text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: replyMarkup);
    }

    public static async Task AnswerCallbackAsync(Update update, string? text = null, bool showAlert = false)
    {
        if (BotClient == null || update.CallbackQuery == null) return;
        try
        {
            await BotClient.AnswerCallbackQuery(update.CallbackQuery.Id, text, showAlert);
        }
        catch { }
    }

    public static async Task SendPhotoAsync(long chatId, string imageUrl, string caption, InlineKeyboardMarkup? replyMarkup = null)
    {
        if (BotClient == null) return;
        try
        {
            using var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            using var httpClient = new HttpClient(httpHandler);
            using var response = await httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            var file = InputFile.FromStream(stream, Path.GetFileName(new Uri(imageUrl).AbsolutePath));
            await BotClient.SendPhoto(
                chatId: chatId,
                photo: file,
                caption: caption,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: replyMarkup);
        }
        catch
        {
            await BotClient.SendMessage(
                chatId: chatId,
                text: caption,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: replyMarkup);
        }
    }

    public static InlineKeyboardMarkup CreateInlineKeyboard(params (string Text, string CallbackData)[] buttons)
    {
        var rows = new List<List<InlineKeyboardButton>>();
        foreach (var (text, callback) in buttons)
        {
            rows.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(text, callback)
            });
        }
        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup CreateInlineKeyboardRow(params (string Text, string CallbackData)[] buttons)
    {
        var row = new List<InlineKeyboardButton>();
        foreach (var (text, callback) in buttons)
        {
            row.Add(InlineKeyboardButton.WithCallbackData(text, callback));
        }
        return new InlineKeyboardMarkup(new[] { row });
    }

    public static ReplyKeyboardMarkup CreateMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("/catalog") },
            new[] { new KeyboardButton("/search") },
            new[] { new KeyboardButton("/cart"), new KeyboardButton("/orders") },
            new[] { new KeyboardButton("/profile") }
        })
        { ResizeKeyboard = true };
    }

    public static ReplyKeyboardMarkup CreateAdminMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("/catalog") },
            new[] { new KeyboardButton("/search") },
            new[] { new KeyboardButton("/cart"), new KeyboardButton("/orders") },
            new[] { new KeyboardButton("/profile") },
            new[] { new KeyboardButton("/admin") }
        })
        { ResizeKeyboard = true };
    }
}
