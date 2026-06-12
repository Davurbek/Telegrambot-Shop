using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Data.Repositories;

namespace TelegramShopBot.Data.Migrations;

public static class SeedAdmin
{
    public static async Task SeedAsync(string connectionString, string email, string passwordHash, string fullName)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var existing = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM AdminUsers WHERE Email = @Email", new { Email = email });
        if (existing > 0) return;

        await conn.ExecuteAsync(@"
            INSERT INTO AdminUsers (Email, PasswordHash, FullName, Role, IsActive, CreatedAt)
            VALUES (@Email, @PasswordHash, @FullName, 'SuperAdmin', 1, @CreatedAt)",
            new
            {
                Email = email,
                PasswordHash = passwordHash,
                FullName = fullName,
                CreatedAt = DateTime.UtcNow
            });
    }
}
