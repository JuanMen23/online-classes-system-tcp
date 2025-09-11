using Common.Config;
using Server;

Console.WriteLine("Starting Server Application...");

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
    Console.WriteLine($"Fatal error: {ex.Message}");
}
finally
{
    serverApp.Stop();
}
