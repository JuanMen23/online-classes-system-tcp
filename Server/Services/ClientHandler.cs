using System.Net.Sockets;
using Common.Protocol;
using Server.ClassSession;

namespace Server.Services;

/// <summary>
/// Handles individual client connections and communication using the protocol
/// </summary>
public class ClientHandler
{
    private readonly Socket _clientSocket;
    private readonly ClientManager _clientManager;
    private readonly ClientState _state;
    private readonly ProtocolHandler _protocolHandler;
    private readonly UserService _userService;
    private readonly ClassService _classService;

    public Guid Id { get; }

    public ClientHandler(Socket clientSocket)
    {
        _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
        _clientManager = ClientManager.Instance;
        _state = new ClientState();
        _protocolHandler = new ProtocolHandler();
        _classService = ClassService.Instance;
        _userService = UserService.Instance;
        Id = Guid.NewGuid();
    }

    public void HandleClient()
    {
        RegisterClient();

        try
        {
            ProcessClientMessages();
        }
        catch (SocketException ex)
        {
            HandleSocketException(ex);
        }
        catch (ObjectDisposedException)
        {
            HandleObjectDisposedException();
        }
        catch (Exception ex)
        {
            HandleGenericException(ex);
        }
        finally
        {
            CleanupClient();
        }
    }

    private void RegisterClient()
    {
        _clientManager.AddClient(this);
    }

    private void ProcessClientMessages()
    {
        while (_state.IsConnected)
        {
            try
            {
                var receivedMessage = _protocolHandler.ReceiveMessage(_clientSocket);
                Console.WriteLine($"Recibido: {receivedMessage}");

                ProcessCommand(receivedMessage);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Connection closed"))
            {
                _state.MarkAsDisconnectedNaturally();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionAborted)
            {
                Console.WriteLine("Servidor se cerró - desconectando cliente");
                _state.MarkAsDisconnectedNaturally();
            }
            catch (Exception ex) when (ex.Message.Contains("Software caused connection abort"))
            {
                _state.MarkAsDisconnectedNaturally();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recibiendo mensaje del protocolo: {ex.Message}");
                _state.MarkAsDisconnectedNaturally();
            }
        }
    }

    private void ProcessCommand(ProtocolMessage message)
    {
        string responseData;

        switch (message.Command)
        {
            case ProtocolConstants.CMD_REGISTER:
                var partsReg = message.Data.Split('|');
                responseData = _userService.RegisterUser(partsReg[0], partsReg[1]);
                break;

            case ProtocolConstants.CMD_LOGIN:
                var partsLog = message.Data.Split('|');
                responseData = _userService.LoginUser(Id, partsLog[0], partsLog[1]);
                break;

            case ProtocolConstants.CMD_LOGOUT:
                _userService.LogoutUser(Id);
                responseData = ProtocolConstants.RESPONSE_OK;
                break;

            case ProtocolConstants.CMD_CREATE_CLASS:
                HandleCreateClass(message);
                return; 

            default:
                responseData = "Comando desconocido";
                break;
        }

        var responseMessage = new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            message.Command,
            responseData
        );
        _protocolHandler.SendMessage(_clientSocket, responseMessage);
    }

    private void HandleCreateClass(ProtocolMessage message)
    {
        if (!_userService.IsUserLoggedIn(Id))
        {
            SendErrorResponse("Acción no permitida. Debes iniciar sesión primero.");
            return;
        }

        try
        {
            var parts = message.Data.Split('|');
            if (parts.Length < 5) 
            {
                SendErrorResponse("Datos insuficientes para crear la clase");
                return;
            }
            
            var name = parts[0];
            var description = parts[1];
            
            if (!int.TryParse(parts[2], out var maxSeats) || maxSeats <= 0)
            {
                SendErrorResponse("Número de cupos inválido");
                return;
            }
            if (!DateTime.TryParse(parts[3], out var startDateTime))
            {
                SendErrorResponse("Fecha inválida");
                return;
            }
            if (!int.TryParse(parts[4], out var durationMinutes) || durationMinutes <= 0)
            {
                SendErrorResponse("Duración inválida");
                return;
            }

            var imageBase64 = parts.Length > 5 ? parts[5] : null;
            
            var createdClass = _classService.CreateClassWithDetails(
                name, description, maxSeats, startDateTime, durationMinutes, imageBase64
            );
            
            var response = new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_CREATE_CLASS,
                $"OK|{createdClass.Id}|{createdClass.Link}"
            );
            _protocolHandler.SendMessage(_clientSocket, response);
        }
        catch (ArgumentException ex)
        {
            SendErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            SendErrorResponse($"Error inesperado: {ex.Message}");
        }
    }

    private void SendErrorResponse(string errorMessage)
    {
        var errorResponse = new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            ProtocolConstants.CMD_ERROR,
            errorMessage
        );
        _protocolHandler.SendMessage(_clientSocket, errorResponse);
        Console.WriteLine($"[ERROR] {errorMessage}");
    }

    private void HandleSocketException(SocketException ex)
    {
        if (_state.ShouldShowDisconnectMessages())
        {
            Console.WriteLine($"Cliente desconectado abruptamente: {ex.Message}");
        }
    }

    private void HandleObjectDisposedException()
    {
        if (_state.ShouldShowDisconnectMessages())
        {
            Console.WriteLine("Socket del cliente fue liberado");
        }
    }

    private void HandleGenericException(Exception ex)
    {
        Console.WriteLine($"Error manejando cliente: {ex.Message}");
    }

    private void CleanupClient()
    {
        _clientManager.RemoveClient(this);

        if (_state.IsConnected)
        {
            DisconnectClient();
        }
    }

    public void DisconnectClient()
    {
        if (!_state.IsConnected)
        {
            return;
        }

        _state.MarkAsDisconnectedByServer();
        CloseSocket();
    }

    private void CloseSocket()
    {
        try
        {
            _clientSocket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // Ignore if already closed
        }
        finally
        {
            _clientSocket.Close();
        }
    }
}
