using Dapper;
using Microsoft.Data.SqlClient;
using TelegramShopBot.Domain.Models;

namespace TelegramShopBot.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IUnitOfWork _uow;
    private readonly string _connectionString;

    public UserRepository(IUnitOfWork uow, string connectionString)
    {
        _uow = uow;
        _connectionString = connectionString;
    }

    public async Task<UserProfile?> GetByTelegramIdAsync(long telegramId)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try { return await conn.QueryFirstOrDefaultAsync<UserProfile>("SELECT * FROM Users WHERE TelegramId = @Id", new { Id = telegramId }); }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<UserProfile> CreateAsync(UserProfile user)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            await conn.ExecuteAsync(@"INSERT INTO Users (TelegramId, Username, FirstName, LastName, PhoneNumber, DeliveryAddress, RegistrationDate, IsAdmin) VALUES (@TelegramId, @Username, @FirstName, @LastName, @PhoneNumber, @DeliveryAddress, @RegistrationDate, @IsAdmin)", user, transaction: _uow.SharedTransaction);
            return user;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<UserProfile> UpdateAsync(UserProfile user)
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            await conn.ExecuteAsync(@"UPDATE Users SET Username=@Username, FirstName=@FirstName, LastName=@LastName, PhoneNumber=@PhoneNumber, DeliveryAddress=@DeliveryAddress, IsAdmin=@IsAdmin WHERE TelegramId=@TelegramId", user, transaction: _uow.SharedTransaction);
            return user;
        }
        finally { if (ownConn) conn.Dispose(); }
    }

    public async Task<List<UserProfile>> GetAdminsAsync()
    {
        SqlConnection? conn; bool ownConn; if (_uow.SharedConnection is SqlConnection sharedConn) { conn = sharedConn; ownConn = false; } else { conn = new SqlConnection(_connectionString); await conn.OpenAsync(); ownConn = true; }
        try
        {
            var result = await conn.QueryAsync<UserProfile>("SELECT * FROM Users WHERE IsAdmin = 1");
            return result.ToList();
        }
        finally { if (ownConn) conn.Dispose(); }
    }
}

