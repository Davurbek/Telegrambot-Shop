using Microsoft.Data.SqlClient;

namespace TelegramShopBot.Data.Migrations;

public static class DatabaseSchema
{
    public static async Task InitializeAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();

        cmd.CommandText = @"
            IF OBJECT_ID(N'dbo.UserProfile', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Users', N'U') IS NULL
                EXEC sp_rename 'UserProfile', 'Users';

            IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
            BEGIN
                CREATE TABLE Users (
                    TelegramId BIGINT PRIMARY KEY,
                    Username NVARCHAR(255),
                    FirstName NVARCHAR(255) NOT NULL,
                    LastName NVARCHAR(255),
                    PhoneNumber NVARCHAR(50),
                    DeliveryAddress NVARCHAR(500),
                    RegistrationDate DATETIME2 NOT NULL,
                    IsAdmin BIT NOT NULL DEFAULT 0
                );
            END;

            IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
            BEGIN
                CREATE TABLE Categories (
                    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL UNIQUE,
                    Description NVARCHAR(MAX)
                );
            END;

            IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
            BEGIN
                CREATE TABLE Products (
                    ProductId INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL,
                    Description NVARCHAR(MAX),
                    Price DECIMAL(18,2) NOT NULL CHECK(Price > 0),
                    CategoryId INT NOT NULL,
                    ImageUrl NVARCHAR(500),
                    StockQuantity INT NOT NULL CHECK(StockQuantity >= 0),
                    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
                );
            END;

            IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
            BEGIN
                CREATE TABLE Orders (
                    OrderId INT IDENTITY(1,1) PRIMARY KEY,
                    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
                    CustomerId BIGINT NOT NULL,
                    TotalAmount DECIMAL(18,2) NOT NULL,
                    Status NVARCHAR(50) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    UpdatedAt DATETIME2,
                    RejectionReason NVARCHAR(MAX),
                    TrackingNumber NVARCHAR(255),
                    EstimatedDeliveryDate DATETIME2,
                    FOREIGN KEY (CustomerId) REFERENCES Users(TelegramId)
                );

                CREATE TABLE OrderItems (
                    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
                    OrderId INT NOT NULL,
                    ProductId INT NOT NULL,
                    ProductName NVARCHAR(255) NOT NULL,
                    UnitPrice DECIMAL(18,2) NOT NULL,
                    Quantity INT NOT NULL,
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId),
                    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
                );

                CREATE TABLE Payments (
                    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
                    OrderId INT NOT NULL UNIQUE,
                    TransactionId NVARCHAR(255) NOT NULL UNIQUE,
                    Amount DECIMAL(18,2) NOT NULL,
                    PaymentDate DATETIME2 NOT NULL,
                    ClickPaymentLink NVARCHAR(MAX),
                    FOREIGN KEY (OrderId) REFERENCES Orders(OrderId)
                );
            END;

            IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE AuditLogs (
                    LogId INT IDENTITY(1,1) PRIMARY KEY,
                    Timestamp DATETIME2 NOT NULL,
                    UserId BIGINT,
                    Action NVARCHAR(255) NOT NULL,
                    Details NVARCHAR(MAX),
                    ErrorMessage NVARCHAR(MAX)
                );
            END;

            IF OBJECT_ID(N'dbo.AdminUsers', N'U') IS NULL
            BEGIN
                CREATE TABLE AdminUsers (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Email NVARCHAR(255) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(MAX) NOT NULL,
                    FullName NVARCHAR(255) NOT NULL,
                    Role NVARCHAR(50) NOT NULL DEFAULT 'Admin',
                    IsActive BIT NOT NULL DEFAULT 1,
                    CreatedAt DATETIME2 NOT NULL,
                    LastLoginAt DATETIME2
                );
            END;

            IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
            BEGIN
                CREATE TABLE RefreshTokens (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Token NVARCHAR(500) NOT NULL UNIQUE,
                    AdminId INT NOT NULL,
                    CreatedAt DATETIME2 NOT NULL,
                    ExpiresAt DATETIME2 NOT NULL,
                    RevokedAt DATETIME2,
                    FOREIGN KEY (AdminId) REFERENCES AdminUsers(Id)
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_products_categoryid' AND object_id = OBJECT_ID(N'dbo.Products'))
                CREATE INDEX idx_products_categoryid ON Products(CategoryId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_cartitems_customerid' AND object_id = OBJECT_ID(N'dbo.CartItems'))
                CREATE INDEX idx_cartitems_customerid ON CartItems(CustomerId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_orders_customerid' AND object_id = OBJECT_ID(N'dbo.Orders'))
                CREATE INDEX idx_orders_customerid ON Orders(CustomerId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_orders_status' AND object_id = OBJECT_ID(N'dbo.Orders'))
                CREATE INDEX idx_orders_status ON Orders(Status);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_orders_ordernumber' AND object_id = OBJECT_ID(N'dbo.Orders'))
                CREATE INDEX idx_orders_ordernumber ON Orders(OrderNumber);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_products_name' AND object_id = OBJECT_ID(N'dbo.Products'))
                CREATE INDEX idx_products_name ON Products(Name);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_refreshtokens_adminid' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
                CREATE INDEX idx_refreshtokens_adminid ON RefreshTokens(AdminId);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'idx_refreshtokens_token' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
                CREATE INDEX idx_refreshtokens_token ON RefreshTokens(Token);
        ";

        await cmd.ExecuteNonQueryAsync();
    }
}
