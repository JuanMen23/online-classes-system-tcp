using System.Net.Sockets;
using Common.Protocol;
using Server.ClassSession;
using Server.Services;

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
    private readonly ClassService _classService;

    public Guid Id { get; }

    public ClientHandler(Socket clientSocket)
    {
        _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
        _clientManager = ClientManager.Instance;
        _state = new ClientState();
        _protocolHandler = new ProtocolHandler();
        _classService = ClassService.Instance;
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
            switch (message.Command)
            {
                case ProtocolConstants.CMD_CREATE_CLASS:
                    HandleCreateClass(message);
                    break;

                default:
                    var echoResponse = new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        message.Command,
                        $"Echo: {message.Data}"
                    );
                    _protocolHandler.SendMessage(_clientSocket, echoResponse);
                    Console.WriteLine($"Respuesta enviada: {echoResponse}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando comando: {ex.Message}");

            try
            {
                var errorMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Internal server error"
                );
                _protocolHandler.SendMessage(_clientSocket, errorMessage);
            }
            catch
            {
                Console.WriteLine("Error al enviar respuesta de error");
            }
        }
    }

    private void HandleCreateClass(ProtocolMessage message)
    {
        // Expected data: name|description|maxSeats|startDateTime|duration|imageBase64
        var parts = message.Data.Split('|');
        string name = parts[0];
        string description = parts[1];
        int maxSeats = int.Parse(parts[2]);
        DateTime startDateTime = DateTime.Parse(parts[3]);
        int durationMinutes = int.Parse(parts[4]);
        string? imageBase64 = parts.Length > 5 ? parts[5] : null;

        string? imagePath = null;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            Directory.CreateDirectory("Images");
            imagePath = Path.Combine("Images", $"{Guid.NewGuid()}.png");
            File.WriteAllBytes(imagePath, Convert.FromBase64String(imageBase64));
        }

        var createdClass = _classService.CreateClass(name, description, maxSeats, startDateTime, durationMinutes, imagePath);

        var response = new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            ProtocolConstants.CMD_CREATE_CLASS,
            $"OK|{createdClass.Id}|{createdClass.Link}"
        );

        _protocolHandler.SendMessage(_clientSocket, response);
        Console.WriteLine($"Clase creada: {createdClass.Id} ({createdClass.Name})");
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
