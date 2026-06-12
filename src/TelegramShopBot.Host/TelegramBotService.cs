using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TelegramShopBot.Host.Handlers;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host;

public class TelegramBotService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly IBotLogger _logger;
    private ITelegramBotClient? _botClient;
    private CancellationTokenSource? _cts;

    public TelegramBotService(IServiceScopeFactory scopeFactory, IServiceProvider services, IConfiguration configuration, IBotLogger logger)
    {
        _scopeFactory = scopeFactory;
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var botToken = _configuration["Telegram:BotToken"]
            ?? throw new InvalidOperationException("Bot token not configured");

        _botClient = new TelegramBotClient(botToken);
        BotHelper.BotClient = _botClient;

        await _botClient.DeleteWebhook(cancellationToken: cancellationToken);

        using (var scope = _scopeFactory.CreateScope())
        {
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>() as NotificationService;
            if (notificationService != null)
            {
                notificationService.SetSendMessageFunc(async (chatId, text) =>
                {
                    if (_botClient != null)
                        await _botClient.SendMessage(chatId, text, parseMode: ParseMode.Markdown);
                });
            }
        }

        var updateHandler = new BotUpdateHandler(_services);
        var receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _botClient.StartReceiving(
            updateHandler.HandleUpdateAsync,
            (client, ex, ct) =>
            {
                _logger.LogError(ex, "Polling error");
                return Task.CompletedTask;
            },
            receiverOptions,
            _cts.Token
        );

        var me = await _botClient.GetMe(_cts.Token);
        _logger.LogInformation($"Bot started: @{me.Username}");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        _logger.LogInformation("Bot stopped");
        return Task.CompletedTask;
    }
}
