using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Server.Services;

/// <summary>
/// Servicio simple para publicar logs a RabbitMQ
/// Tolerante a fallos: si RabbitMQ no está disponible, solo loguea en consola
/// </summary>
public class LoggingService
{
    private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());
    public static LoggingService Instance => _instance.Value;

    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();

    private LoggingService()
    {
        Connect();
    }

    private void Connect()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
                Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "admin",
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "admin"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare("logs-exchange", ExchangeType.Topic, durable: true);
            _channel.QueueDeclare("logs-queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("logs-queue", "logs-exchange", "log.*");

            Console.WriteLine("[LoggingService] ✅ Conectado a RabbitMQ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoggingService] ⚠️ No se pudo conectar a RabbitMQ: {ex.Message}");
            // No lanzar excepción - continuamos sin RabbitMQ
        }
    }

    /// <summary>Publica un log a RabbitMQ</summary>
    public void PublishLog(
        string evento,
        string? usuario,
        string mensaje,
        string nivel = "INFO",
        int? claseId = null,
        Dictionary<string, string>? metadata = null)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            Console.WriteLine($"[LoggingService] 📝 (sin RabbitMQ) {evento} - {usuario}");
            return;
        }

        try
        {
            var log = new
            {
                timestamp = DateTime.UtcNow,
                usuario = string.IsNullOrWhiteSpace(usuario) ? "system" : usuario,
                evento,
                nivel,
                claseId,
                mensaje,
                metadata = metadata != null
                    ? new Dictionary<string, string>(metadata)
                    : new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(log);
            var body = Encoding.UTF8.GetBytes(json);
            var routingKey = $"log.{evento.Replace(".", "_")}"; // log.user_logged_in

            lock (_lock)
            {
                _channel.BasicPublish("logs-exchange", routingKey, null, body);
            }

            Console.WriteLine($"[LoggingService] 📤 {evento} → {usuario}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoggingService] ❌ Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}

