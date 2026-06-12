using Telegram.Bot.Types;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Handlers;

public class AdminCommandHandler : ICommandHandler
{
    public async Task ExecuteAsync(Update update, UserProfile user, IServiceProvider services)
    {
        var userService = services.GetRequiredService<IUserService>();
        var chatId = update.Message?.Chat.Id ?? user.TelegramId;

        if (!await userService.IsAdminAsync(user.TelegramId))
        {
            await BotHelper.SendMessageAsync(chatId, "⛔ Access denied. Admin privileges required.");
            return;
        }

        var text = update.Message?.Text ?? "";

        if (text == "/admin")
        {
            await ShowAdminMenu(chatId);
            return;
        }

        if (text.StartsWith("/addproduct"))
            await HandleAddProduct(update, chatId, services);
        else if (text.StartsWith("/addcategory"))
            await HandleAddCategory(update, chatId, services);
        else if (text.StartsWith("/approve"))
            await HandleApproveOrder(update, chatId, services);
        else if (text.StartsWith("/reject"))
            await HandleRejectOrder(update, chatId, services);
        else if (text.StartsWith("/ship"))
            await HandleShipOrder(update, chatId, services);
        else if (text.StartsWith("/deliver"))
            await HandleDeliverOrder(update, chatId, services);
        else if (text.StartsWith("/stats"))
            await HandleStats(chatId, services);
        else if (text.StartsWith("/pending"))
            await HandlePendingOrders(chatId, services);
        else
            await ShowAdminMenu(chatId);
    }

    private async Task ShowAdminMenu(long chatId)
    {
        var menu = @"⚙️ **Admin Panel**

📦 /pending - View Pending Orders
➕ /addproduct - Add Product
📂 /addcategory - Add Category
✅ /approve ORD-XXX - Approve Order
❌ /reject ORD-XXX reason - Reject Order
🚚 /ship ORD-XXX tracking - Mark Shipped
📭 /deliver ORD-XXX - Mark Delivered
📊 /stats - View Statistics";

        await BotHelper.SendMessageAsync(chatId, menu);
    }

    private async Task HandleAddProduct(Update update, long chatId, IServiceProvider services)
    {
        var productService = services.GetRequiredService<IProductService>();
        var parts = update.Message?.Text?.Split('|') ?? Array.Empty<string>();

        if (parts.Length < 6)
        {
            await BotHelper.SendMessageAsync(chatId,
                "Usage: /addproduct Product Name | Description | 99999 | 1 | image_url | 10");
            return;
        }

        try
        {
            var name = parts[0].Replace("/addproduct", "").Trim();
            var desc = parts[1].Trim();
            var price = decimal.Parse(parts[2].Trim());
            var categoryId = int.Parse(parts[3].Trim());
            var imageUrl = parts[4].Trim();
            var stock = int.Parse(parts[5].Trim());

            var product = await productService.CreateProductAsync(name, desc, price, categoryId, imageUrl, stock);
            await BotHelper.SendMessageAsync(chatId, $"✅ Product created: {product.Name} (ID: {product.ProductId})");
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private async Task HandleAddCategory(Update update, long chatId, IServiceProvider services)
    {
        var productService = services.GetRequiredService<IProductService>();
        var parts = update.Message?.Text?.Split('|') ?? Array.Empty<string>();

        if (parts.Length < 2)
        {
            await BotHelper.SendMessageAsync(chatId, "Usage: /addcategory Category Name | Description (optional)");
            return;
        }

        try
        {
            var name = parts[0].Replace("/addcategory", "").Trim();
            var desc = parts.Length > 1 ? parts[1].Trim() : null;
            var category = await productService.CreateCategoryAsync(name, desc);
            await BotHelper.SendMessageAsync(chatId, $"✅ Category created: {category.Name} (ID: {category.CategoryId})");
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private async Task HandleApproveOrder(Update update, long chatId, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var parts = update.Message?.Text?.Split(' ') ?? Array.Empty<string>();

        if (parts.Length < 2)
        {
            await BotHelper.SendMessageAsync(chatId, "Usage: /approve ORD-20241220-00001");
            return;
        }

        try
        {
            var orderNum = parts[1].Trim();
            var order = await orderService.GetOrderByNumberAsync(orderNum);
            if (order == null) { await BotHelper.SendMessageAsync(chatId, "❌ Order not found."); return; }

            await orderService.ApproveOrderAsync(order.OrderId, orderNum, update.Message?.From?.Id ?? 0);
            await BotHelper.SendMessageAsync(chatId, $"✅ Order {orderNum} approved!");

            var paymentLink = await GetPaymentLinkAsync(services, orderNum, order.TotalAmount);
            await notificationService.NotifyOrderApprovedAsync(orderNum, order.CustomerId, paymentLink);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private static async Task<string?> GetPaymentLinkAsync(IServiceProvider services, string orderNumber, decimal amount)
    {
        try
        {
            var paymentService = services.GetRequiredService<IPaymentService>();
            return await paymentService.GeneratePaymentLinkAsync(orderNumber, amount, 0);
        }
        catch
        {
            return null;
        }
    }

    private async Task HandleRejectOrder(Update update, long chatId, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var parts = update.Message?.Text?.Split(' ') ?? Array.Empty<string>();

        if (parts.Length < 3)
        {
            await BotHelper.SendMessageAsync(chatId, "Usage: /reject ORD-20241220-00001 Reason for rejection");
            return;
        }

        try
        {
            var orderNum = parts[1].Trim();
            var reason = string.Join(" ", parts.Skip(2));
            var order = await orderService.GetOrderByNumberAsync(orderNum);
            if (order == null) { await BotHelper.SendMessageAsync(chatId, "❌ Order not found."); return; }

            await orderService.RejectOrderAsync(order.OrderId, orderNum, update.Message?.From?.Id ?? 0, reason);
            await BotHelper.SendMessageAsync(chatId, $"✅ Order {orderNum} rejected.");

            await notificationService.NotifyOrderRejectedAsync(orderNum, order.CustomerId, reason);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private async Task HandleShipOrder(Update update, long chatId, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var parts = update.Message?.Text?.Split(' ') ?? Array.Empty<string>();

        if (parts.Length < 3)
        {
            await BotHelper.SendMessageAsync(chatId, "Usage: /ship ORD-20241220-00001 TRACK123");
            return;
        }

        try
        {
            var orderNum = parts[1].Trim();
            var tracking = parts[2].Trim();
            var order = await orderService.GetOrderByNumberAsync(orderNum);
            if (order == null) { await BotHelper.SendMessageAsync(chatId, "❌ Order not found."); return; }

            await orderService.MarkAsShippedAsync(order.OrderId, tracking, DateTime.UtcNow.AddDays(7));
            await BotHelper.SendMessageAsync(chatId, $"✅ Order {orderNum} marked as shipped. Tracking: {tracking}");

            await notificationService.NotifyOrderStatusChangeAsync(orderNum, "InDelivery", order.CustomerId);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private async Task HandleDeliverOrder(Update update, long chatId, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var parts = update.Message?.Text?.Split(' ') ?? Array.Empty<string>();

        if (parts.Length < 2)
        {
            await BotHelper.SendMessageAsync(chatId, "Usage: /deliver ORD-20241220-00001");
            return;
        }

        try
        {
            var orderNum = parts[1].Trim();
            var order = await orderService.GetOrderByNumberAsync(orderNum);
            if (order == null) { await BotHelper.SendMessageAsync(chatId, "❌ Order not found."); return; }

            await orderService.MarkAsDeliveredAsync(order.OrderId);
            await BotHelper.SendMessageAsync(chatId, $"✅ Order {orderNum} marked as delivered!");

            await notificationService.NotifyOrderStatusChangeAsync(orderNum, "Delivered", order.CustomerId);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private async Task HandleStats(long chatId, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var productService = services.GetRequiredService<IProductService>();

        try
        {
            var stats = await orderService.GetStatisticsAsync();
            var productCount = await productService.GetTotalProductCountAsync();
            var topProducts = await orderService.GetTopSellingProductsAsync(5);

            var msg = $@"📊 **Sales Statistics**

📦 Total Orders: {stats.TotalOrders}
💰 Total Revenue: {stats.TotalRevenue:N0} UZS
📱 Total Products: {productCount}
📊 Average Order Value: {(stats.TotalOrders > 0 ? stats.TotalRevenue / stats.TotalOrders : 0):N0} UZS

**Orders by Status:**
{string.Join("\n", stats.OrdersByStatus.Select(kv => $"  {kv.Key}: {kv.Value}"))}

**Top Selling Products:**
{string.Join("\n", topProducts.Select((p, i) => $"  {i + 1}. {p.ProductName} - {p.TotalQuantitySold} sold"))}";

            await BotHelper.SendMessageAsync(chatId, msg);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }

    private async Task HandlePendingOrders(long chatId, IServiceProvider services)
    {
        var orderService = services.GetRequiredService<IOrderService>();
        var userService = services.GetRequiredService<IUserService>();

        try
        {
            var orders = await orderService.GetOrdersByStatusAsync(OrderStatus.Pending);
            if (!orders.Any())
            {
                await BotHelper.SendMessageAsync(chatId, "✅ No pending orders.");
                return;
            }

            var msg = "⏳ **Pending Orders**\n\n";
            foreach (var order in orders.Take(10))
            {
                var customer = await userService.GetUserProfileAsync(order.CustomerId);
                var phone = customer?.PhoneNumber ?? "N/A";
                msg += $"• {order.OrderNumber} — {order.TotalAmount:N0} UZS (📞 {phone})\n";
            }

            await BotHelper.SendMessageAsync(chatId, msg);
        }
        catch (Exception ex)
        {
            await BotHelper.SendMessageAsync(chatId, $"❌ {ex.Message}");
        }
    }
}
