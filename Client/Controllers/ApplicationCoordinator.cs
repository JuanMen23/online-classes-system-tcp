using Common.Protocol;
using Client.Services;
using Common;
using System.Threading.Tasks;

namespace Client.Controllers;

/// <summary>
/// Coordinates the main application flow and manages the overall application state
/// </summary>
public class ApplicationCoordinator
{
    private readonly SocketService _socketService;
    private readonly MenuManager _menuManager;
    private readonly AuthController _authController;
    private readonly ClassController _classController;
    private readonly ResponseHandler _responseHandler;
    
    private bool _isRunning;
    private volatile bool _waitingForServerResponse;
    
    private readonly ProtocolHandler _protocolHandler = new();

    public ApplicationCoordinator(SocketService socketService, MenuManager menuManager, 
        AuthController authController, ClassController classController)
    {
        _socketService = socketService ?? throw new ArgumentNullException(nameof(socketService));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _authController = authController ?? throw new ArgumentNullException(nameof(authController));
        _classController = classController ?? throw new ArgumentNullException(nameof(classController));
        _responseHandler = new ResponseHandler(_menuManager, _authController.AuthManager, SetWaitingForResponse);
    }

    private void SetWaitingForResponse(bool waiting)
    {
        _waitingForServerResponse = waiting;
    }

    /// <summary>
    /// Starts the main application loop
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            _menuManager.ShowConnectionStatus("Iniciando aplicación del cliente...");
            _menuManager.ShowConnectionStatus("Conectando al servidor...");

            _socketService.Connect();

            _menuManager.ShowConnectionStatus("¡Conectado al servidor!");
            _isRunning = true;

            _ = ReceiveMessagesAsync();
            
            await RunAsync();
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al iniciar el cliente: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Main application loop
    /// </summary>
    private async Task RunAsync()
    {
        while (_isRunning)
        {
            if (_waitingForServerResponse)
            {
                await Task.Delay(100);
                continue;
            }

            Console.WriteLine();

            if (!_authController.IsLoggedIn)
            {
                await ShowLoggedOutMenuAsync();
            }
            else
            {
                await ShowLoggedInMenuAsync();
            }
        }
    }

    /// <summary>
    /// Shows the menu for users who are not logged in
    /// </summary>
    private async Task ShowLoggedOutMenuAsync()
    {
        _menuManager.ShowLoggedOutMenu();
        string? choice = _menuManager.ReadLine();

        switch (choice)
        {
            case "1": 
                await _authController.HandleRegisterAsync(_socketService, SetWaitingForResponse); 
                break;
            case "2": 
                await _authController.HandleLoginAsync(_socketService, SetWaitingForResponse); 
                break;
            case "3": 
                _isRunning = false; 
                break;
            default: 
                PrintMessage.Error("Opción no válida."); 
                break;
        }
    }

    /// <summary>
    /// Shows the menu for logged in users
    /// </summary>
    private async Task ShowLoggedInMenuAsync()
    {
        _menuManager.ShowLoggedInMenu(_authController.CurrentUser ?? "");
        string? choice = _menuManager.ReadLine();

        if (string.IsNullOrEmpty(choice)) return;

        string cleanChoice = choice.Trim().ToLower();
        
        if (cleanChoice.StartsWith("descargar"))
        {
            var parts = cleanChoice.Split(' ');
            
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[1]))
            {
                await _classController.DownloadImageAsync(_socketService, SetWaitingForResponse, parts[1]);
            }
            else
            {
                PrintMessage.Error("Comando incompleto. Uso correcto: descargar + ID de clase");
            }
            return;
        }

        switch (choice)
        {
            case "1":
                await _classController.CreateClassAsync(_socketService, SetWaitingForResponse);
                break;
            case "2":
                await _classController.ModifyClassAsync(_socketService, SetWaitingForResponse);
                break;
            case "3":
                await _classController.DeleteClassAsync(_socketService, SetWaitingForResponse);
                break;
            case "4":
                await _classController.RequestClassListAsync(_socketService, SetWaitingForResponse);
                break;
            case "5":
                await _classController.EnrollInClassAsync(_socketService, SetWaitingForResponse);
                break;
            case "6":
                await _classController.CancelEnrollmentAsync(_socketService, SetWaitingForResponse);
                break;
            case "7":
                await _classController.SearchClassesAsync(_socketService, SetWaitingForResponse);
                break;
            case "8":
                await _classController.RequestHistoryAsync(_socketService, SetWaitingForResponse);
                break;
            case "9":
                await _authController.HandleLogoutAsync(_socketService, SetWaitingForResponse);
                break;
            default:
                PrintMessage.Error("Opción no válida.");
                break;
        }
    }

    /// <summary>
    /// Receives and processes messages from the server
    /// </summary>
    private async Task ReceiveMessagesAsync()
    {
        try
        {
            while (_isRunning)
            {
                ProtocolMessage? response = await _socketService.ReceiveMessageAsync();

                if (response == null)
                {
                    _responseHandler.HandleServerDisconnection();
                    _isRunning = false;
                    break;
                }

                _responseHandler.HandleResponse(response);
            }
        }
        catch (Exception)
        {
            if (_isRunning)
            {
                _responseHandler.HandleConnectionLoss();
                _isRunning = false;
            }
        }
    }

    /// <summary>
    /// Disconnects from the server
    /// </summary>
    private void Disconnect()
    {
        _menuManager.ShowConnectionStatus("Desconectando...");
        _socketService.Disconnect();
        _menuManager.ShowConnectionStatus("Desconectado");
    }
}
