using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/images")]
public class ImagesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly string _baseUrl;

    public ImagesController(IWebHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        _baseUrl = configuration["BaseUrl"]?.TrimEnd('/') ?? "https://localhost:44337";
    }

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.ErrorResult("No file provided"));

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(ApiResponse<object>.ErrorResult("Invalid file type. Allowed: PNG, JPEG, WebP"));

        if (file.Length > 5 * 1024 * 1024)
            return StatusCode(413, ApiResponse<object>.ErrorResult("File exceeds 5 MB limit"));

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            ImageUrl = $"{_baseUrl}/api/v1/images/{fileName}",
            ThumbnailUrl = $"{_baseUrl}/api/v1/images/{fileName}",
            FileName = fileName,
            FileSize = file.Length
        }));
    }

    [HttpGet("{filename}")]
    [AllowAnonymous]
    public IActionResult GetImage(string filename)
    {
        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");

        var fileName = Path.GetFileName(filename);
        var filePath = Path.Combine(uploadsDir, fileName);

        if (!System.IO.File.Exists(filePath) || fileName != filename)
            return NotFound();

        var contentType = filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
            : filename.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
            : "image/jpeg";

        return PhysicalFile(filePath, contentType);
    }
}
