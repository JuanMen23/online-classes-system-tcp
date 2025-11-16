using System.Collections.Generic;
using System.Net.Sockets;
using Common.Protocol;
using System.Threading.Tasks;

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

    public async Task HandleClientAsync()
    {
        RegisterClient();

        try { await ProcessClientMessagesAsync(); }
        
        catch (SocketException ex) { HandleSocketException(ex); }
        
        catch (ObjectDisposedException) { HandleObjectDisposedException(); }
        
        catch (InvalidOperationException ex) when (ex.Message.Contains("closed"))
        {
            _state.MarkAsDisconnectedNaturally();
        }
        
        catch (Exception ex) { HandleGenericException(ex); }
        
        finally { CleanupClient(); }
    }

    private void RegisterClient()
    {
        _clientManager.AddClient(this);
    }

    private async Task ProcessClientMessagesAsync()
    {
        while (_state.IsConnected)
        {
            try
            {
                var receivedMessage = await _protocolHandler.ReceiveMessageAsync(_clientSocket);
                Console.WriteLine($"Recibido: {receivedMessage}");

                await ProcessCommandAsync(receivedMessage);
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

    private async Task ProcessCommandAsync(ProtocolMessage message)
    {
        try
        {
            string responseData;

            switch (message.Command)
            {
                case ProtocolConstants.CMD_REGISTER:
                    var partsReg = message.Data.Split('|');
                    responseData = _userService.RegisterUser(partsReg[0], partsReg[1], Id);
                    await SendResponseAsync(message.Command, responseData);
                    break;

                case ProtocolConstants.CMD_LOGIN:
                    var partsLog = message.Data.Split('|');
                    responseData = _userService.LoginUser(Id, partsLog[0], partsLog[1]);
                    await SendResponseAsync(message.Command, responseData);
                    break;

                case ProtocolConstants.CMD_LOGOUT:
                    _userService.LogoutUser(Id);
                    await SendResponseAsync(message.Command, ProtocolConstants.RESPONSE_OK);
                    break;

                case ProtocolConstants.CMD_CREATE_CLASS:
                    var createResponse = _classService.HandleCreateClass(message.Data, Id);
                    await _protocolHandler.SendMessageAsync(_clientSocket, createResponse);
                    break;

                case ProtocolConstants.CMD_MODIFY_CLASS:
                    var modifyResponse = _classService.HandleModifyClass(message.Data, Id);
                    await _protocolHandler.SendMessageAsync(_clientSocket, modifyResponse);
                    LogClassCommandResult("class_modify", modifyResponse, ExtractClassId(message.Data));
                    break;

                case ProtocolConstants.CMD_LIST_CLASSES:
                    var listResponse = _classService.HandleListClasses();
                    await _protocolHandler.SendMessageAsync(_clientSocket, listResponse);
                    var listCount = string.IsNullOrWhiteSpace(listResponse.Data)
                        ? 0
                        : listResponse.Data.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                    LogClassCommandResult(
                        "class_list",
                        listResponse,
                        null,
                        new Dictionary<string, string> { ["count"] = listCount.ToString() },
                        "Listado de clases consultado");
                    break;

                case ProtocolConstants.CMD_ENROLL_CLASS:
                    var enrollResponse = _classService.HandleEnrollClass(message.Data, Id);
                    await _protocolHandler.SendMessageAsync(_clientSocket, enrollResponse);
                    LogClassCommandResult("class_enroll", enrollResponse, ExtractClassId(message.Data));
                    break;

                case ProtocolConstants.CMD_CANCEL_ENROLL:
                    var cancelResponse = _classService.HandleCancelEnrollment(message.Data, Id);
                    await _protocolHandler.SendMessageAsync(_clientSocket, cancelResponse);
                    LogClassCommandResult("class_cancel_enroll", cancelResponse, ExtractClassId(message.Data));
                    break;

                case ProtocolConstants.CMD_DELETE_CLASS:
                    var deleteResponse = _classService.HandleDeleteClass(message.Data, Id);
                    await _protocolHandler.SendMessageAsync(_clientSocket, deleteResponse);
                    LogClassCommandResult("class_delete", deleteResponse, ExtractClassId(message.Data));
                    break;
                
                case ProtocolConstants.CMD_SEARCH_CLASSES:
                    var searchResponse = _classService.HandleSearchClasses(message.Data);
                    await _protocolHandler.SendMessageAsync(_clientSocket, searchResponse);
                    var filters = message.Data.Split('|');
                    var searchMetadata = new Dictionary<string, string>
                    {
                        ["keyword"] = filters.Length > 0 ? filters[0] : string.Empty,
                        ["min_date"] = filters.Length > 1 ? filters[1] : string.Empty,
                        ["max_date"] = filters.Length > 2 ? filters[2] : string.Empty,
                        ["max_duration"] = filters.Length > 3 ? filters[3] : string.Empty
                    };
                    LogClassCommandResult("class_search", searchResponse, null, searchMetadata);
                    break;
                
                case ProtocolConstants.CMD_HISTORY:
                    var historyResponse = _classService.HandleHistory(Id);
                    await _protocolHandler.SendMessageAsync(_clientSocket, historyResponse);
                    var historyCount = string.IsNullOrWhiteSpace(historyResponse.Data)
                        ? 0
                        : historyResponse.Data.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
                    LogClassCommandResult(
                        "class_history",
                        historyResponse,
                        null,
                        new Dictionary<string, string> { ["entries"] = historyCount.ToString() });
                    break;
                
                case ProtocolConstants.CMD_DOWNLOAD_IMAGE:
                    await HandleDownloadImageRequestAsync(message);
                    break;

                default:
                    await SendResponseAsync(message.Command, "Comando desconocido");
                    break;
            }
        }
        catch (Exception ex)
        {
            // Fallback general error
            await SendErrorResponseAsync($"Error inesperado: {ex.Message}");
        }
    }
    
    private async Task HandleDownloadImageRequestAsync(ProtocolMessage message)
    {
        try
        {
            if (!_userService.IsUserLoggedIn(Id)) 
                throw new InvalidOperationException("Debes iniciar sesión para hacer esto.");

            if (!int.TryParse(message.Data, out int classId)) 
                throw new ArgumentException("ID de clase inválido.");

            string imageBase64 = _classService.GetClassImageAsBase64(classId);
            await SendResponseAsync(message.Command, imageBase64);
            var username = _userService.GetLoggedInUsername(Id) ?? "guest";
            LoggingService.Instance.PublishLog(
                "image_download_success",
                username,
                $"Descargó imagen de la clase {classId}",
                "INFO",
                classId,
                new Dictionary<string, string>
                {
                    ["client_id"] = Id.ToString()
                });
        }
        catch (Exception ex)
        {
            LoggingService.Instance.PublishLog(
                "image_download_error",
                _userService.GetLoggedInUsername(Id) ?? "guest",
                ex.Message,
                "WARN",
                metadata: new Dictionary<string, string> { ["client_id"] = Id.ToString() });
            await SendErrorResponseAsync(ex.Message);
        }
    }

    private async Task SendResponseAsync(int command, string data)
    {
        var responseMessage = new ProtocolMessage(ProtocolConstants.HEADER_RESPONSE, command, data);
        await _protocolHandler.SendMessageAsync(_clientSocket, responseMessage);
    }

    private async Task SendErrorResponseAsync(string errorMessage)
    {
        var errorResponse = new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            ProtocolConstants.CMD_ERROR,
            errorMessage
        );
        await _protocolHandler.SendMessageAsync(_clientSocket, errorResponse);
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

    private static int? ExtractClassId(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        var separatorIndex = payload.IndexOf('|');
        var candidate = separatorIndex >= 0 ? payload[..separatorIndex] : payload;

        return int.TryParse(candidate, out var classId) ? classId : null;
    }

    private void LogClassCommandResult(string eventoBase, ProtocolMessage response, int? classId = null, Dictionary<string, string>? extra = null, string? customMessage = null)
    {
        var metadata = extra != null ? new Dictionary<string, string>(extra) : new Dictionary<string, string>();
        metadata["client_id"] = Id.ToString();

        if (classId.HasValue)
        {
            metadata["class_id"] = classId.Value.ToString();
        }

        var success = response.Command != ProtocolConstants.CMD_ERROR &&
                      !response.Data.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase);

        var evento = success ? $"{eventoBase}_success" : $"{eventoBase}_error";
        var nivel = success ? "INFO" : "WARN";
        var usuario = _userService.GetLoggedInUsername(Id) ?? "guest";

        LoggingService.Instance.PublishLog(evento, usuario, customMessage ?? response.Data, nivel, classId, metadata);
    }
}
