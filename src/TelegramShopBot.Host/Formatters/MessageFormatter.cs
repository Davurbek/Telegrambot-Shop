using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Host.Formatters;

public class MessageFormatter : IMessageFormatter
{
    public string FormatWelcomeMessage(UserProfile user)
    {
        return $@"👋 Welcome to Electronics Shop Bot, {user.FirstName}!

Choose an option:
🛍️ /catalog - Browse Catalog
🔍 /search - Search Products
🛒 /cart - View Cart
📦 /orders - My Orders
👤 /profile - My Profile";
    }

    public string FormatCategoryList(List<Category> categories)
    {
        var lines = new List<string> { "📂 **Categories**\n" };
        for (int i = 0; i < categories.Count; i++)
        {
            lines.Add($"{i + 1}. {categories[i].Name}");
            if (!string.IsNullOrEmpty(categories[i].Description))
                lines.Add($"   _{categories[i].Description}_");
        }
        return string.Join("\n", lines) + "\n\nSelect a category to browse products.";
    }

    public string FormatProductList(List<Product> products, int page, int totalPages)
    {
        if (!products.Any()) return "No products found.";

        var lines = new List<string> { "📱 **Products**\n" };
        foreach (var p in products)
        {
            var status = p.IsAvailable ? "✅ In Stock" : "❌ Unavailable";
            lines.Add($"• **{p.Name}** - {p.Price:N0} UZS ({status})");
        }

        lines.Add($"\nPage {page} of {totalPages}");
        return string.Join("\n", lines);
    }

    public string FormatProductDetails(Product product)
    {
        var availability = product.IsAvailable
            ? $"✅ In Stock ({product.StockQuantity} available)"
            : "❌ Out of Stock";

        var text = $@"**{product.Name}**

{product.Description ?? "No description"}

💰 **Price:** {product.Price:N0} UZS
📦 **Stock:** {availability}
🏷️ **Category ID:** {product.CategoryId}";

        if (!string.IsNullOrEmpty(product.ImageUrl))
            text += $"\n🖼️ [View Image]({product.ImageUrl})";

        return text;
    }

    public string FormatCart(List<CartItem> items, decimal total)
    {
        if (!items.Any()) return "🛒 Your cart is empty.";

        var lines = new List<string> { "🛒 **Your Cart**\n" };
        foreach (var item in items)
        {
            lines.Add($"• **{item.Product?.Name ?? "Unknown"}**");
            lines.Add($"  {item.Quantity} × {item.Product?.Price:N0} UZS = {item.TotalPrice:N0} UZS");
        }

        lines.Add($"\n**Total: {total:N0} UZS**");
        return string.Join("\n", lines);
    }

    public string FormatOrderHistory(List<Order> orders)
    {
        if (!orders.Any()) return "📦 You have no orders yet.";

        var lines = new List<string> { "📦 **Order History**\n" };
        foreach (var order in orders)
        {
            lines.Add($"• **{order.OrderNumber}**");
            lines.Add($"  Date: {order.CreatedAt:yyyy-MM-dd HH:mm}");
            lines.Add($"  Amount: {order.TotalAmount:N0} UZS");
            lines.Add($"  Status: {FormatStatus(order.Status)}");
            lines.Add("");
        }
        return string.Join("\n", lines);
    }

    public string FormatOrderDetails(Order order)
    {
        var text = $@"📋 **Order {order.OrderNumber}**

**Status:** {FormatStatus(order.Status)}
**Date:** {order.CreatedAt:yyyy-MM-dd HH:mm}
**Total:** {order.TotalAmount:N0} UZS

**Items:**";

        foreach (var item in order.OrderItems)
        {
            text += $"\n• {item.ProductName} × {item.Quantity} = {item.TotalPrice:N0} UZS";
        }

        if (!string.IsNullOrEmpty(order.TrackingNumber))
            text += $"\n\n📦 **Tracking:** {order.TrackingNumber}";

        if (order.EstimatedDeliveryDate.HasValue)
            text += $"\n📅 **Estimated Delivery:** {order.EstimatedDeliveryDate:yyyy-MM-dd}";

        if (!string.IsNullOrEmpty(order.RejectionReason))
            text += $"\n\n❌ **Rejection Reason:** {order.RejectionReason}";

        return text;
    }

    public string FormatProfile(UserProfile user)
    {
        var text = $@"👤 **Your Profile**

**Name:** {user.FullName}
**Username:** @{user.Username ?? "N/A"}
**Phone:** {user.PhoneNumber ?? "Not set"}
**Address:** {user.DeliveryAddress ?? "Not set"}
**Registered:** {user.RegistrationDate:yyyy-MM-dd}";

        return text;
    }

    private static string FormatStatus(OrderStatus status) => status switch
    {
        OrderStatus.Pending => "⏳ Pending",
        OrderStatus.Approved => "✅ Approved",
        OrderStatus.Rejected => "❌ Rejected",
        OrderStatus.Paid => "💰 Paid",
        OrderStatus.InDelivery => "🚚 In Delivery",
        OrderStatus.Delivered => "📦 Delivered",
        OrderStatus.Cancelled => "🚫 Cancelled",
        _ => status.ToString()
    };
}
