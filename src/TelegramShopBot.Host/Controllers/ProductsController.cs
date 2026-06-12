using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] int? categoryId = null, [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null, [FromQuery] bool? inStockOnly = null,
        [FromQuery] string? search = null)
    {
        List<Domain.Models.Product> items;
        int totalCount;

        if (!string.IsNullOrEmpty(search))
        {
            var all = await _productService.SearchProductsAsync(search);
            totalCount = all.Count;
            items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }
        else if (categoryId.HasValue)
        {
            (items, totalCount) = await _productService.GetProductsByCategoryAsync(categoryId.Value, page, pageSize);
        }
        else
        {
            var all = await _productService.SearchProductsAsync("");
            totalCount = all.Count;
            items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        if (minPrice.HasValue) items = items.Where(p => p.Price >= minPrice.Value).ToList();
        if (maxPrice.HasValue) items = items.Where(p => p.Price <= maxPrice.Value).ToList();
        if (inStockOnly == true) items = items.Where(p => p.IsAvailable).ToList();

        var dtos = items.Select(p => new ProductDto
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            ImageUrl = p.ImageUrl,
            StockQuantity = p.StockQuantity,
            IsAvailable = p.IsAvailable
        }).ToList();

        var result = new PagedResult<ProductDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<PagedResult<ProductDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(int id)
    {
        var p = await _productService.GetProductByIdAsync(id);
        if (p == null)
            return NotFound(ApiResponse<ProductDto>.ErrorResult($"Product {id} not found"));

        return Ok(ApiResponse<ProductDto>.SuccessResult(new ProductDto
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            ImageUrl = p.ImageUrl,
            StockQuantity = p.StockQuantity,
            IsAvailable = p.IsAvailable
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<ProductDto>.ErrorResult("Name is required", new() { "Name is required" }));
        if (request.Price <= 0)
            return BadRequest(ApiResponse<ProductDto>.ErrorResult("Price must be positive", new() { "Price must be positive" }));
        if (request.StockQuantity < 0)
            return BadRequest(ApiResponse<ProductDto>.ErrorResult("Stock cannot be negative", new() { "Stock cannot be negative" }));

        var p = await _productService.CreateProductAsync(
            request.Name, request.Description, request.Price,
            request.CategoryId, request.ImageUrl, request.StockQuantity);

        return CreatedAtAction(nameof(GetProduct), new { id = p.ProductId },
            ApiResponse<ProductDto>.SuccessResult(new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                IsAvailable = p.IsAvailable
            }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<ProductDto>.ErrorResult("Name is required"));
        if (request.Price <= 0)
            return BadRequest(ApiResponse<ProductDto>.ErrorResult("Price must be positive"));

        var p = await _productService.UpdateProductAsync(
            id, request.Name, request.Description, request.Price,
            request.CategoryId, request.ImageUrl, request.StockQuantity);

        if (p == null)
            return NotFound(ApiResponse<ProductDto>.ErrorResult($"Product {id} not found"));

        return Ok(ApiResponse<ProductDto>.SuccessResult(new ProductDto
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            ImageUrl = p.ImageUrl,
            StockQuantity = p.StockQuantity,
            IsAvailable = p.IsAvailable
        }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(int id)
    {
        await _productService.DeleteProductAsync(id);
        return Ok(ApiResponse<object>.SuccessResult(null, "Product deleted"));
    }
}

public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; }
}

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public int StockQuantity { get; set; } = 1;
}

public class UpdateProductRequest : CreateProductRequest { }
