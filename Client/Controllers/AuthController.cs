using Common.Protocol;
using Client.Services;
using Common;

namespace Client.Controllers;

/// <summary>
/// Handles all authentication-related operations including login, register, and logout
/// </summary>
public class AuthController
{
    private readonly MenuManager _menuManager;
    private readonly InputValidator _inputValidator;
    private readonly AuthManager _authManager;

    public AuthController(MenuManager menuManager, InputValidator inputValidator, AuthManager authManager)
    {
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
    }

    /// <summary>
    /// Gets the AuthManager instance for external access
    /// </summary>
    public AuthManager AuthManager => _authManager;

    /// <summary>
    /// Checks if a user is currently logged in
    /// </summary>
    public bool IsLoggedIn => _authManager.IsLoggedIn;

    /// <summary>
    /// Gets the current logged in username
    /// </summary>
    public string? CurrentUser => _authManager.CurrentUser;

    /// <summary>
    /// Handles user registration process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task HandleRegisterAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var (username, password) = _menuManager.PromptRegistration();

            if (!_inputValidator.ValidateCredentials(username, password))
            {
                PrintMessage.Error("El usuario y la contraseña no pueden estar vacíos.");
                return;
            }

            string data = _inputValidator.FormatCredentials(username, password);
            var message = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_REGISTER,
                data
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(message);
            PrintMessage.Information("Enviando datos de registro...");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error durante el registro: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles user login process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task HandleLoginAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var (username, password) = _menuManager.PromptLogin();

            if (!_inputValidator.ValidateCredentials(username, password))
            {
                PrintMessage.Error("El usuario y la contraseña no pueden estar vacíos.");
                return;
            }

            _authManager.AttemptLogin(username);
            string data = _inputValidator.FormatCredentials(username, password);
            var message = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_LOGIN,
                data
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(message);
            PrintMessage.Information("Iniciando sesión...");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error durante el login: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles user logout process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task HandleLogoutAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var message = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_LOGOUT,
                ""
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(message);
            PrintMessage.Information("Cerrando sesión...");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error durante el logout: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates user credentials without sending to server
    /// </summary>
    /// <param name="username">Username to validate</param>
    /// <param name="password">Password to validate</param>
    /// <returns>True if credentials are valid, false otherwise</returns>
    public bool ValidateCredentials(string username, string password)
    {
        return _inputValidator.ValidateCredentials(username, password);
    }

    /// <summary>
    /// Gets the current authentication status
    /// </summary>
    /// <returns>Status message</returns>
    public string GetAuthStatus()
    {
        return _authManager.GetStatusMessage();
    }
}
