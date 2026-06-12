using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TelegramShopBot.Data.Migrations;
using TelegramShopBot.Data.Repositories;
using TelegramShopBot.Host;
using TelegramShopBot.Host.Formatters;
using TelegramShopBot.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? "DefaultSecretKeyThatIsAtLeast32BytesLong!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TelegramShopBot";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AdminPanel";
var connectionString = builder.Configuration["Database:ConnectionString"] ?? "Data Source=shopbot.db";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

// Database
await DatabaseSchema.InitializeAsync(connectionString);
await SeedData.SeedAsync(connectionString);

// Seed default admin user
var adminEmail = builder.Configuration["Admin:DefaultEmail"] ?? "admin@shopbot.com";
var adminPassword = builder.Configuration["Admin:DefaultPassword"] ?? "admin123";
var adminName = builder.Configuration["Admin:DefaultFullName"] ?? "Super Admin";
var hasher = new PasswordHasher();
var adminHash = hasher.HashPassword(adminPassword);
await SeedAdmin.SeedAsync(connectionString, adminEmail, adminHash, adminName);

// Services
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IRefreshTokenRepository>(sp => sp.GetRequiredService<IUnitOfWork>().RefreshTokens);
builder.Services.AddScoped<IJwtTokenService>(sp =>
{
    var refreshRepo = sp.GetRequiredService<IRefreshTokenRepository>();
    return new JwtTokenService(refreshRepo, jwtSecret, jwtIssuer, jwtAudience);
});

// Bot logger
builder.Services.AddSingleton<IBotLogger, ConsoleLogger>();

// Notification service
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Payment service
var clickConfig = builder.Configuration.GetSection("Click");
builder.Services.AddScoped<IPaymentService>(sp =>
{
    var uow = sp.GetRequiredService<IUnitOfWork>();
    return new PaymentService(uow,
        clickConfig["ServiceId"] ?? "",
        clickConfig["MerchantId"] ?? "",
        clickConfig["SecretKey"] ?? "",
        clickConfig["ReturnUrl"] ?? ""
    );
});

// Message formatter
builder.Services.AddSingleton<IMessageFormatter, MessageFormatter>();

// Bot background service
builder.Services.AddHostedService<TelegramBotService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminPanel", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.ErrorResult("Rate limit exceeded"));
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AdminPanel");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("api");;

app.Run();
