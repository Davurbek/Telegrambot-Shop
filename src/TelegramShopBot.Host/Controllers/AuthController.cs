using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Domain.Models;
using TelegramShopBot.Services;

namespace TelegramShopBot.Host.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _uow;
    private readonly IAuditLogger _auditLogger;

    public AuthController(IAuthService authService, IJwtTokenService jwtTokenService, IUnitOfWork uow, IAuditLogger auditLogger)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
        _uow = uow;
        _auditLogger = auditLogger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginRequest request)
    {
        var admin = await _authService.ValidateCredentialsAsync(request.Email, request.Password);

        if (admin == null)
        {
            HttpContext.Items["ClientIP"] = HttpContext.Connection.RemoteIpAddress?.ToString();
            return Unauthorized(ApiResponse<object>.ErrorResult("Invalid credentials"));
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(admin);
        var refreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(admin.Id);

        await _auditLogger.LogAdminActionAsync(admin.Id, "Login", "Admin logged in via API");

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = 900,
            AdminUser = new
            {
                admin.Id,
                admin.Email,
                admin.FullName,
                admin.Role
            }
        }));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var storedToken = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken);

        if (storedToken == null || storedToken.IsExpired || storedToken.IsRevoked)
            return Unauthorized(ApiResponse<object>.ErrorResult("Invalid or expired refresh token"));

        var admin = await _authService.GetAdminByIdAsync(storedToken.AdminId);
        if (admin == null)
            return Unauthorized(ApiResponse<object>.ErrorResult("Admin not found"));

        var newAccessToken = _jwtTokenService.GenerateAccessToken(admin);

        await _auditLogger.LogAdminActionAsync(admin.Id, "TokenRefresh", "Access token refreshed");

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            AccessToken = newAccessToken,
            ExpiresIn = 900
        }));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest request)
    {
        var storedToken = await _uow.RefreshTokens.GetByTokenAsync(request.RefreshToken);
        if (storedToken != null && !storedToken.IsRevoked)
        {
            await _uow.RefreshTokens.RevokeAsync(storedToken.Id);
        }

        return Ok(ApiResponse<object>.SuccessResult(null, "Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetCurrentUser()
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (adminIdClaim == null || !int.TryParse(adminIdClaim, out var adminId))
            return Unauthorized(ApiResponse<object>.ErrorResult("Invalid token"));

        var admin = await _authService.GetAdminByIdAsync(adminId);
        if (admin == null)
            return NotFound(ApiResponse<object>.ErrorResult("Admin not found"));

        return Ok(ApiResponse<object>.SuccessResult(new
        {
            admin.Id,
            admin.Email,
            admin.FullName,
            admin.Role
        }));
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
