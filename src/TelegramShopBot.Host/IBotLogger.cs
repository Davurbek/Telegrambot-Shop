namespace TelegramShopBot.Host;

public interface IBotLogger
{
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(Exception ex, string message);
}

public class ConsoleLogger : IBotLogger
{
    public void LogInformation(string message) =>
        Console.WriteLine($"[INFO] {DateTime.UtcNow:HH:mm:ss} {message}");

    public void LogWarning(string message) =>
        Console.WriteLine($"[WARN] {DateTime.UtcNow:HH:mm:ss} {message}");

    public void LogError(Exception ex, string message) =>
        Console.WriteLine($"[ERROR] {DateTime.UtcNow:HH:mm:ss} {message}: {ex.Message}");
}
