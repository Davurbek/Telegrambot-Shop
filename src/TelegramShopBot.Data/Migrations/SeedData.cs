using Microsoft.Data.SqlClient;
using Dapper;

namespace TelegramShopBot.Data.Migrations;

public static class SeedData
{
    public static async Task SeedAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var categoryCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Categories");
        if (categoryCount > 0) return;

        using var tx = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(@"
            INSERT INTO Categories (Name, Description) VALUES
            ('Smartphones', 'Mobile phones and smartphones'),
            ('Tablets', 'Tablet computers'),
            ('Laptops', 'Laptop computers'),
            ('Audio', 'Audio equipment and headphones'),
            ('Smart_Home_Devices', 'Smart home automation devices')
        ", transaction: tx);

        await connection.ExecuteAsync(@"
            INSERT INTO Products (Name, Description, Price, CategoryId, ImageUrl, StockQuantity) VALUES
            ('iPhone 16 Pro', 'Apple iPhone 16 Pro with A18 Pro chip', 14999000, 1, NULL, 15),
            ('Samsung Galaxy S25', 'Samsung Galaxy S25 with Snapdragon 8 Gen 4', 11999000, 1, NULL, 20),
            ('Xiaomi 14 Pro', 'Xiaomi 14 Pro with Leica optics', 8999000, 1, NULL, 25),
            ('Google Pixel 9 Pro', 'Google Pixel 9 Pro with Tensor G4', 10999000, 1, NULL, 10),
            ('OnePlus 13', 'OnePlus 13 with Hasselblad camera', 9499000, 1, NULL, 18),

            ('iPad Pro M4', 'Apple iPad Pro with M4 chip 13-inch', 15999000, 2, NULL, 12),
            ('Samsung Galaxy Tab S10', 'Samsung Galaxy Tab S10 Ultra', 12999000, 2, NULL, 14),
            ('Xiaomi Pad 7 Pro', 'Xiaomi Pad 7 Pro with Snapdragon 8 Gen 3', 6999000, 2, NULL, 20),
            ('Amazon Fire HD 11', 'Amazon Fire HD 11 entertainment tablet', 3499000, 2, NULL, 30),

            ('MacBook Pro 16 M4', 'Apple MacBook Pro 16-inch with M4 Max', 29999000, 3, NULL, 8),
            ('Dell XPS 16', 'Dell XPS 16 Intel Core Ultra 9', 24999000, 3, NULL, 10),
            ('Lenovo ThinkPad X1', 'Lenovo ThinkPad X1 Carbon Gen 13', 21999000, 3, NULL, 7),
            ('ASUS ROG Zephyrus G16', 'ASUS ROG Zephyrus G16 gaming laptop', 27999000, 3, NULL, 9),
            ('HP Spectre x360', 'HP Spectre x360 2-in-1 laptop', 19999000, 3, NULL, 11),

            ('Sony WH-1000XM6', 'Sony WH-1000XM6 wireless noise cancelling', 3499000, 4, NULL, 22),
            ('AirPods Pro 3', 'Apple AirPods Pro 3 with USB-C', 2499000, 4, NULL, 35),
            ('Samsung Galaxy Buds3 Pro', 'Samsung Galaxy Buds3 Pro with AKG tuning', 1999000, 4, NULL, 28),
            ('JBL Charge 6', 'JBL Charge 6 portable Bluetooth speaker', 1499000, 4, NULL, 18),

            ('Philips Hue Starter Kit', 'Philips Hue smart lighting starter kit', 2499000, 5, NULL, 16),
            ('Nest Thermostat 5', 'Google Nest Learning Thermostat 5th gen', 3499000, 5, NULL, 12)
        ", transaction: tx);

        await tx.CommitAsync();
    }
}
