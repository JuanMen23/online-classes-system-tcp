using Common.Config;
using Client;

Console.WriteLine("Starting Client Application...");

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
    Console.WriteLine($"Fatal error: {ex.Message}");
}
