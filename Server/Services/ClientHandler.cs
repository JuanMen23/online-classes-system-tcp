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
        try
        {
            string responseData;

            switch (message.Command)
            {
                case ProtocolConstants.CMD_REGISTER:
                    var partsReg = message.Data.Split('|');
                    responseData = _userService.RegisterUser(partsReg[0], partsReg[1]);
                    SendResponse(message.Command, responseData);
                    break;

                case ProtocolConstants.CMD_LOGIN:
                    var partsLog = message.Data.Split('|');
                    responseData = _userService.LoginUser(Id, partsLog[0], partsLog[1]);
                    SendResponse(message.Command, responseData);
                    break;

                case ProtocolConstants.CMD_LOGOUT:
                    _userService.LogoutUser(Id);
                    SendResponse(message.Command, ProtocolConstants.RESPONSE_OK);
                    break;

                case ProtocolConstants.CMD_CREATE_CLASS:
                    var createResponse = _classService.HandleCreateClass(message.Data, Id);
                    _protocolHandler.SendMessage(_clientSocket, createResponse);
                    break;

                case ProtocolConstants.CMD_LIST_CLASSES:
                    var listResponse = _classService.HandleListClasses();
                    _protocolHandler.SendMessage(_clientSocket, listResponse);
                    break;

                case ProtocolConstants.CMD_ENROLL_CLASS:
                    var enrollResponse = _classService.HandleEnrollClass(message.Data, Id);
                    _protocolHandler.SendMessage(_clientSocket, enrollResponse);
                    break;

                default:
                    SendResponse(message.Command, "Comando desconocido");
                    break;
            }
        }
        catch (Exception ex)
        {
            // Fallback general error
            SendErrorResponse($"Error inesperado: {ex.Message}");
        }
    }


    private void SendResponse(int command, string data)
    {
        var responseMessage = new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            command,
            data
        );
        _protocolHandler.SendMessage(_clientSocket, responseMessage);
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
