using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;

    public ProductService(IUnitOfWork uow) => _uow = uow;

    public async Task<Category> CreateCategoryAsync(string name, string? description)
    {
        var category = new Category { Name = name, Description = description };
        return await _uow.Categories.CreateAsync(category);
    }

    public async Task<Category?> UpdateCategoryAsync(int categoryId, string name, string? description)
    {
        var cat = await _uow.Categories.GetByIdAsync(categoryId);
        if (cat == null) return null;
        cat.Name = name;
        cat.Description = description;
        return await _uow.Categories.UpdateAsync(cat);
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        var count = await _uow.Categories.GetProductCountAsync(categoryId);
        if (count > 0) throw new InvalidOperationException("Cannot delete category with existing products");
        await _uow.Categories.DeleteAsync(categoryId);
    }

    public async Task<List<Category>> GetAllCategoriesAsync() =>
        await _uow.Categories.GetAllAsync();

    public async Task<(List<Category> Items, int TotalCount)> GetCategoriesPagedAsync(int page, int pageSize)
    {
        var items = await _uow.Categories.GetPagedAsync(page, pageSize);
        var total = await _uow.Categories.GetTotalCountAsync();
        return (items, total);
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId) =>
        await _uow.Categories.GetByIdAsync(categoryId);

    public async Task<int> GetProductCountByCategoryAsync(int categoryId) =>
        await _uow.Categories.GetProductCountAsync(categoryId);

    public async Task<Product> CreateProductAsync(string name, string? description, decimal price, int categoryId, string? imageUrl, int stockQuantity)
    {
        if (price <= 0) throw new ArgumentException("Price must be positive");
        if (stockQuantity < 0) throw new ArgumentException("Stock quantity cannot be negative");

        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            CategoryId = categoryId,
            ImageUrl = imageUrl,
            StockQuantity = stockQuantity
        };
        return await _uow.Products.CreateAsync(product);
    }

    public async Task<Product?> UpdateProductAsync(int productId, string name, string? description, decimal price, int categoryId, string? imageUrl, int stockQuantity)
    {
        if (price <= 0) throw new ArgumentException("Price must be positive");
        if (stockQuantity < 0) throw new ArgumentException("Stock quantity cannot be negative");

        var product = await _uow.Products.GetByIdAsync(productId);
        if (product == null) return null;

        product.Name = name;
        product.Description = description;
        product.Price = price;
        product.CategoryId = categoryId;
        product.ImageUrl = imageUrl;
        product.StockQuantity = stockQuantity;
        return await _uow.Products.UpdateAsync(product);
    }

    public async Task DeleteProductAsync(int productId) =>
        await _uow.Products.DeleteAsync(productId);

    public async Task<Product?> GetProductByIdAsync(int productId) =>
        await _uow.Products.GetByIdAsync(productId);

    public async Task<(List<Product> Items, int TotalCount)> GetProductsByCategoryAsync(int categoryId, int page = 1, int pageSize = 10)
    {
        var items = await _uow.Products.GetByCategoryAsync(categoryId, page, pageSize);
        var total = await _uow.Products.GetCountByCategoryAsync(categoryId);
        return (items, total);
    }

    public async Task<List<Product>> SearchProductsAsync(string query) =>
        await _uow.Products.SearchAsync(query);

    public async Task<bool> IsProductAvailableAsync(int productId, int quantity)
    {
        var product = await _uow.Products.GetByIdAsync(productId);
        return product != null && product.StockQuantity >= quantity;
    }

    public async Task<int> GetTotalProductCountAsync() =>
        await _uow.Products.GetTotalCountAsync();
}
