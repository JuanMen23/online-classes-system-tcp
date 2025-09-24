using Common.Config;
using Client;

Console.WriteLine("Iniciando aplicación del cliente...");

// Create configuration
var config = new AppConfig();

// Create and start the client
var clientApp = new ClientApplication(config);

try
{
    clientApp.Start();
}
catch (Exception ex)
{
    Console.WriteLine($"Error fatal: {ex.Message}");
}
