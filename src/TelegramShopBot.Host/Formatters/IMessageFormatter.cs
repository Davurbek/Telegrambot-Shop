using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Host.Formatters;

public interface IMessageFormatter
{
    string FormatWelcomeMessage(UserProfile user);
    string FormatCategoryList(List<Category> categories);
    string FormatProductList(List<Product> products, int page, int totalPages);
    string FormatProductDetails(Product product);
    string FormatCart(List<CartItem> items, decimal total);
    string FormatOrderHistory(List<Order> orders);
    string FormatOrderDetails(Order order);
    string FormatProfile(UserProfile user);
}
