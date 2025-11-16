using LogsServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddHostedService<RabbitMQService>();

// URL
builder.WebHost.UseUrls("http://0.0.0.0:5001");

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("✅ LogsServer iniciado en http://0.0.0.0:5001");
Console.WriteLine("📚 Documentación: http://localhost:5001/swagger");

app.Run();

