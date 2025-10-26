using Common.Config;
using Client.Services;
using Client.Controllers;

namespace Client;

/// <summary>
/// Main client application entry point that initializes and starts the application
/// </summary>
public class ClientApplication
{
    private readonly AppConfig _config;
    private readonly ApplicationCoordinator _coordinator;

    public ClientApplication(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        // Initialize services
        var socketService = new SocketService(_config);
        var menuManager = new MenuManager();
        var inputValidator = new InputValidator();
        var authManager = new AuthManager();
        
        // Initialize controllers
        var authController = new AuthController(menuManager, inputValidator, authManager);
        var classController = new ClassController(menuManager, inputValidator);
        
        // Initialize coordinator
        _coordinator = new ApplicationCoordinator(socketService, menuManager, authController, classController);
    }

    /// <summary>
    /// Starts the client application
    /// </summary>
    public async Task StartAsync()
    {
        await _coordinator.StartAsync();
    }
}
