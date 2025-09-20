using Common.Protocol;
using Client.Services;

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
    private volatile bool _waitingForServerResponse = false;

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
    public void Start()
    {
        try
        {
            _menuManager.ShowConnectionStatus("Iniciando aplicación del cliente...");
            _menuManager.ShowConnectionStatus("Conectando al servidor...");

            _socketService.Connect();

            _menuManager.ShowConnectionStatus("¡Conectado al servidor!");
            _isRunning = true;

            // Start thread to receive messages from server
            var receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Main application loop
            Run();
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al iniciar el cliente: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Main application loop
    /// </summary>
    private void Run()
    {
        while (_isRunning)
        {
            if (_waitingForServerResponse)
            {
                Thread.Sleep(100);
                continue;
            }

            Console.WriteLine();

            if (!_authController.IsLoggedIn)
            {
                ShowLoggedOutMenu();
            }
            else
            {
                ShowLoggedInMenu();
            }
        }
    }

    /// <summary>
    /// Shows the menu for users who are not logged in
    /// </summary>
    private void ShowLoggedOutMenu()
    {
        _menuManager.ShowLoggedOutMenu();
        string? choice = _menuManager.ReadLine();

        switch (choice)
        {
            case "1": 
                _authController.HandleRegister(_socketService, SetWaitingForResponse); 
                break;
            case "2": 
                _authController.HandleLogin(_socketService, SetWaitingForResponse); 
                break;
            case "3": 
                _isRunning = false; 
                break;
            default: 
                _menuManager.ShowError("Opción no válida."); 
                break;
        }
    }

    /// <summary>
    /// Shows the menu for logged in users
    /// </summary>
    private void ShowLoggedInMenu()
    {
        _menuManager.ShowLoggedInMenu(_authController.CurrentUser ?? "");
        string? choice = _menuManager.ReadLine();
        
        if (string.IsNullOrEmpty(choice)) return;

        switch (choice)
        {
            case "1":
                _classController.CreateClass(_socketService, SetWaitingForResponse);
                break;
            case "2":
                _classController.RequestClassList(_socketService, SetWaitingForResponse);
                break;
            case "3":
                _classController.EnrollInClass(_socketService, SetWaitingForResponse);
                break;
            case "4":
                _classController.CancelEnrollment(_socketService, SetWaitingForResponse);
                break;
            case "5":
                _authController.HandleLogout(_socketService, SetWaitingForResponse);
                break;
            default:
                _menuManager.ShowError("Opción no válida.");
                break;
        }
    }

    /// <summary>
    /// Receives and processes messages from the server
    /// </summary>
    private void ReceiveMessages()
    {
        try
        {
            while (_isRunning)
            {
                ProtocolMessage? response = _socketService.ReceiveMessage();

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
