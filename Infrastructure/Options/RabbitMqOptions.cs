namespace Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "tradeguard_exchange";
}