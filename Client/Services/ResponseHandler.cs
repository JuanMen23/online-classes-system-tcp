using Common.Protocol;
using Common.Config;

namespace Client.Services;

/// <summary>
/// Handles processing of server responses and delegates to appropriate services
/// </summary>
public class ResponseHandler
{
    private readonly MenuManager _menuManager;
    private readonly AuthManager _authManager;
    private readonly Action<bool> _setWaitingForResponse;

    public ResponseHandler(MenuManager menuManager, AuthManager authManager, Action<bool> setWaitingForResponse)
    {
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
        _setWaitingForResponse = setWaitingForResponse ?? throw new ArgumentNullException(nameof(setWaitingForResponse));
    }

    /// <summary>
    /// Processes a server response and delegates to appropriate handler
    /// </summary>
    /// <param name="response">Protocol message from server</param>
    public void HandleResponse(ProtocolMessage response)
    {
        switch (response.Command)
        {
            case ProtocolConstants.CMD_REGISTER:
                HandleRegisterResponse(response);
                break;

            case ProtocolConstants.CMD_LOGIN:
                HandleLoginResponse(response);
                break;

            case ProtocolConstants.CMD_LOGOUT:
                HandleLogoutResponse(response);
                break;

            case ProtocolConstants.CMD_CREATE_CLASS:
                HandleClassCreationResponse(response);
                break;

            case ProtocolConstants.CMD_MODIFY_CLASS:
                HandleClassModificationResponse(response);
                break;

            case ProtocolConstants.CMD_LIST_CLASSES:
                HandleClassListResponse(response);
                break;

            case ProtocolConstants.CMD_ENROLL_CLASS:
                HandleClassEnrollmentResponse(response);
                break;

            case ProtocolConstants.CMD_CANCEL_ENROLL:
                HandleClassCancellationResponse(response);
                break;

            case ProtocolConstants.CMD_ERROR:
                HandleErrorResponse(response);
                break;

            default:
                HandleUnknownResponse(response);
                break;
        }

        _setWaitingForResponse(false);
    }

    /// <summary>
    /// Handles register response from server
    /// </summary>
    /// <param name="response">Register response message</param>
    private void HandleRegisterResponse(ProtocolMessage response)
    {
        if (response.Data == ProtocolConstants.RESPONSE_OK)
        {
            _menuManager.ShowSuccess("¡Registro exitoso! Ahora puedes iniciar sesión.");
        }
        else
        {
            _menuManager.ShowError($"Error de registro: {response.Data}");
        }
    }

    /// <summary>
    /// Handles login response from server
    /// </summary>
    /// <param name="response">Login response message</param>
    private void HandleLoginResponse(ProtocolMessage response)
    {
        if (response.Data == ProtocolConstants.RESPONSE_OK)
        {
            _authManager.SetLoggedIn(_authManager.CurrentUser ?? "");
            _menuManager.ShowSuccess($"¡Bienvenido, {_authManager.CurrentUser}!");
        }
        else
        {
            _authManager.ClearCurrentUser();
            _menuManager.ShowError($"Error de inicio de sesión: {response.Data}");
        }
    }

    /// <summary>
    /// Handles logout response from server
    /// </summary>
    /// <param name="response">Logout response message</param>
    private void HandleLogoutResponse(ProtocolMessage response)
    {
        _authManager.SetLoggedOut();
        _menuManager.ShowInfo("Sesión cerrada correctamente.");
    }

    /// <summary>
    /// Handles class creation response from server
    /// </summary>
    /// <param name="response">Class creation response message</param>
    private void HandleClassCreationResponse(ProtocolMessage response)
    {
        if (response.Data.StartsWith("OK|"))
        {
            var parts = response.Data.Split('|');
            if (parts.Length >= 3)
            {
                _menuManager.ShowSuccess("¡Clase creada exitosamente!");
                Console.WriteLine($"   ID: {parts[1]}");
                Console.WriteLine($"   Link: {parts[2]}");
            }
            else
            {
                _menuManager.ShowSuccess("¡Clase creada exitosamente!");
            }
        }
        else
        {
            _menuManager.ShowError($"Error al crear clase: {response.Data}");
        }
    }

    /// <summary>
    /// Handles class modification response from server
    /// </summary>
    /// <param name="response">Class modification response message</param>
    private void HandleClassModificationResponse(ProtocolMessage response)
    {
        if (response.Data.StartsWith("OK|"))
        {
            _menuManager.ShowSuccess(response.Data.Substring(3)); // Mostrar el mensaje después de "OK|"
        }
        else
        {
            _menuManager.ShowError($"Error al modificar clase: {response.Data}");
        }
    }

    /// <summary>
    /// Handles class list response from server
    /// </summary>
    /// <param name="response">Class list response message</param>
    private void HandleClassListResponse(ProtocolMessage response)
    {
        _menuManager.DisplayClassList(response.Data);
    }

    /// <summary>
    /// Handles class enrollment response from server
    /// </summary>
    /// <param name="response">Class enrollment response message</param>
    private void HandleClassEnrollmentResponse(ProtocolMessage response)
    {
        if (response.Data.StartsWith("OK|"))
        {
            var parts = response.Data.Split('|');
            var message = parts.Length > 1 ? parts[1] : "Inscripción exitosa";
            _menuManager.ShowSuccess(message);
        }
        else
        {
            _menuManager.ShowError($"Error en la inscripción: {response.Data}");
        }
    }

    /// <summary>
    /// Handles class enrollment cancellation response from server
    /// </summary>
    /// <param name="response">Class cancellation response message</param>
    private void HandleClassCancellationResponse(ProtocolMessage response)
    {
        if (response.Data.StartsWith("OK|"))
        {
            var parts = response.Data.Split('|');
            var message = parts.Length > 1 ? parts[1] : "Cancelación de inscripción exitosa";
            _menuManager.ShowSuccess(message);
        }
        else
        {
            _menuManager.ShowError($"Error al cancelar la inscripción: {response.Data}");
        }
    }

    /// <summary>
    /// Handles error response from server
    /// </summary>
    /// <param name="response">Error response message</param>
    private void HandleErrorResponse(ProtocolMessage response)
    {
        _menuManager.ShowError($"Error: {response.Data}");
    }

    /// <summary>
    /// Handles unknown response from server
    /// </summary>
    /// <param name="response">Unknown response message</param>
    private void HandleUnknownResponse(ProtocolMessage response)
    {
        Console.WriteLine($"Respuesta del servidor (CMD {response.Command}): {response.Data}");
    }

    /// <summary>
    /// Handles server disconnection
    /// </summary>
    public void HandleServerDisconnection()
    {
        _menuManager.ShowConnectionStatus("Servidor desconectado");
        _authManager.SetLoggedOut();
    }

    /// <summary>
    /// Handles connection loss
    /// </summary>
    public void HandleConnectionLoss()
    {
        _menuManager.ShowConnectionStatus("Se perdió la conexión con el servidor. Presione ENTER para salir.");
        _authManager.SetLoggedOut();
    }
}
