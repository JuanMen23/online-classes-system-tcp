using Common.Config;
using Server;

Console.WriteLine("Iniciando aplicación del servidor...");

// Create configuration
var config = new AppConfig();

// Create and start the server
var serverApp = new ServerApplication(config);

try
{
    serverApp.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Error fatal: {ex.Message}");
}
finally
{
    serverApp.Stop();
}
