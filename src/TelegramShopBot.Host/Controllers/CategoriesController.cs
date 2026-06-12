using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly IProductService _productService;

    public CategoriesController(IProductService productService) => _productService = productService;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var categories = await _productService.GetAllCategoriesAsync();
        var dtos = new List<CategoryDto>();

        foreach (var c in categories)
        {
            var count = await _productService.GetProductCountByCategoryAsync(c.CategoryId);
            dtos.Add(new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                ProductCount = count
            });
        }

        return Ok(ApiResponse<List<CategoryDto>>.SuccessResult(dtos));
    }

    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryDto>>>> GetCategoriesPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (items, totalCount) = await _productService.GetCategoriesPagedAsync(page, pageSize);
        var dtos = new List<CategoryDto>();

        foreach (var c in items)
        {
            var count = await _productService.GetProductCountByCategoryAsync(c.CategoryId);
            dtos.Add(new CategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                ProductCount = count
            });
        }

        var result = new PagedResult<CategoryDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return Ok(ApiResponse<PagedResult<CategoryDto>>.SuccessResult(result));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
    {
        var category = await _productService.GetCategoryByIdAsync(id);
        if (category == null)
            return NotFound(ApiResponse<CategoryDto>.ErrorResult($"Category {id} not found"));

        var count = await _productService.GetProductCountByCategoryAsync(id);
        return Ok(ApiResponse<CategoryDto>.SuccessResult(new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            ProductCount = count
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Name is required"));

        var category = await _productService.CreateCategoryAsync(request.Name, request.Description);
        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId },
            ApiResponse<CategoryDto>.SuccessResult(new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                ProductCount = 0
            }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Name is required"));

        var category = await _productService.UpdateCategoryAsync(id, request.Name, request.Description);
        if (category == null)
            return NotFound(ApiResponse<CategoryDto>.ErrorResult($"Category {id} not found"));

        var count = await _productService.GetProductCountByCategoryAsync(id);
        return Ok(ApiResponse<CategoryDto>.SuccessResult(new CategoryDto
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            ProductCount = count
        }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
    {
        try
        {
            await _productService.DeleteCategoryAsync(id);
            return Ok(ApiResponse<object>.SuccessResult(null, "Category deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResult(ex.Message));
        }
    }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest { }
