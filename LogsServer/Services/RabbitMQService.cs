using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using LogsServer.Models;

namespace LogsServer.Services;

/// <summary>
/// BackgroundService que escucha logs desde RabbitMQ
/// </summary>
public class RabbitMQService : BackgroundService
{
    private readonly ILogger<RabbitMQService> _logger;
    private readonly ILogService _logService;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMQService(ILogger<RabbitMQService> logger, ILogService logService)
    {
        _logger = logger;
        _logService = logService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Esperar a que RabbitMQ esté COMPLETAMENTE listo
        // Delay largo para asegurar queue existe ANTES de cualquier publicación
        _logger.LogInformation("⏰ Esperando 15 segundos para inicializar RabbitMQ...");
        await Task.Delay(15000, stoppingToken);
        _logger.LogInformation("⏰ Conectando a RabbitMQ...");

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

            // Declarar exchange y queue
            _channel.ExchangeDeclare("logs-exchange", ExchangeType.Topic, durable: true);
            _channel.QueueDeclare("logs-queue", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("logs-queue", "logs-exchange", "log.*");

            _logger.LogInformation("✅ RabbitMQ conectado");

            // Consumidor
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                try
                {
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var logEntry = JsonSerializer.Deserialize<LogEntry>(json, options);

                    if (logEntry != null)
                    {
                        _logService.AddLog(logEntry);
                        _logger.LogInformation($"📥 Log guardado: {logEntry.Evento} ({logEntry.Usuario})");
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error procesando mensaje");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume("logs-queue", false, consumer);
            _logger.LogInformation("🔄 Escuchando logs desde RabbitMQ con patrón: log.*");
            
            // Log de estado cada 10 segundos
            var lastCount = 0;
            var timer = new System.Timers.Timer(10000);
            timer.Elapsed += (s, e) => 
            {
                _logger.LogInformation("💓 [] RabbitMQService aún activo...");
            };
            timer.Start();

            // Mantener vivo el servicio
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
            
            timer.Stop();
            timer.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 Error en RabbitMQService");
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

