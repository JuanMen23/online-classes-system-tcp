using System.Net.Sockets;
using System.Text;
using Common.Protocol;

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
    
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of the ClientHandler
    /// </summary>
    /// <param name="clientSocket">The client socket to handle</param>
    public ClientHandler(Socket clientSocket)
    {
        _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
        _clientManager = ClientManager.Instance;
        _state = new ClientState();
        _protocolHandler = new ProtocolHandler();
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Handles the client connection and processes incoming messages
    /// </summary>
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

    /// <summary>
    /// Registers the client with the manager
    /// </summary>
    private void RegisterClient()
    {
        _clientManager.AddClient(this);
    }

    /// <summary>
    /// Processes incoming protocol messages from the client
    /// </summary>
    private void ProcessClientMessages()
    {
        while (_state.IsConnected)
        {
            try
            {
                // Receive protocol message from client
                var receivedMessage = _protocolHandler.ReceiveMessage(_clientSocket);
                Console.WriteLine($"Recibido: {receivedMessage}");
                
                // Process the command (for now, just echo back)
                ProcessCommand(receivedMessage);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Connection closed"))
            {
                // Client disconnected gracefully
                _state.MarkAsDisconnectedNaturally();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionAborted)
            {
                Console.WriteLine("Servidor se cerró - desconectando cliente");
                _state.MarkAsDisconnectedNaturally();
            }
            catch (Exception ex) when (ex.Message.Contains("Software caused connection abort"))
            {
                // Conexión abortada por cierre del servidor - esto es esperado, no es un error
                _state.MarkAsDisconnectedNaturally();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recibiendo mensaje del protocolo: {ex.Message}");
                _state.MarkAsDisconnectedNaturally();
            }
        }
    }

    /// <summary>
    /// Processes the received command and sends appropriate response
    /// </summary>
    /// <param name="message">The received protocol message</param>
    private void ProcessCommand(ProtocolMessage message)
    {
        try
        {
            // For now, just echo back the command with a response header
            var responseMessage = new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                message.Command,
                $"Echo: {message.Data}"
            );

            _protocolHandler.SendMessage(_clientSocket, responseMessage);
            Console.WriteLine($"Respuesta enviada: {responseMessage}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando comando: {ex.Message}");
            
            // Send error response
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
                // If we can't send error response, just log it
                Console.WriteLine("Error al enviar respuesta de error");
            }
        }
    }

    /// <summary>
    /// Handles socket exceptions
    /// </summary>
    /// <param name="ex">The socket exception</param>
    private void HandleSocketException(SocketException ex)
    {
        if (_state.ShouldShowDisconnectMessages())
        {
            Console.WriteLine($"Cliente desconectado abruptamente: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles object disposed exceptions
    /// </summary>
    private void HandleObjectDisposedException()
    {
        if (_state.ShouldShowDisconnectMessages())
        {
            Console.WriteLine("Socket del cliente fue liberado");
        }
    }

    /// <summary>
    /// Handles generic exceptions
    /// </summary>
    /// <param name="ex">The exception</param>
    private void HandleGenericException(Exception ex)
    {
        Console.WriteLine($"Error manejando cliente: {ex.Message}");
    }

    /// <summary>
    /// Cleans up the client connection
    /// </summary>
    private void CleanupClient()
    {
        _clientManager.RemoveClient(this);
        
        if (_state.IsConnected)
        {
            DisconnectClient();
        }
    }

    /// <summary>
    /// Disconnects the client cleanly
    /// </summary>
    public void DisconnectClient()
    {
        if (!_state.IsConnected)
        {
            return; // Already disconnected
        }
        
        _state.MarkAsDisconnectedByServer();
        CloseSocket();
    }

    /// <summary>
    /// Closes the client socket
    /// </summary>
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