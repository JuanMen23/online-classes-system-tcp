using System.Net.Sockets;
using System.Text;

namespace Server.Services;

/// <summary>
/// Handles individual client connections and communication
/// </summary>
public class ClientHandler
{
    private readonly Socket _clientSocket;
    private readonly byte[] _buffer;
    private readonly ClientManager _clientManager;
    private readonly ClientState _state;
    
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of the ClientHandler
    /// </summary>
    /// <param name="clientSocket">The client socket to handle</param>
    public ClientHandler(Socket clientSocket)
    {
        _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
        _clientManager = ClientManager.Instance;
        _buffer = new byte[Common.Protocol.ProtocolConstants.MAX_BUFFER_SIZE];
        _state = new ClientState();
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
    /// Processes incoming messages from the client
    /// </summary>
    private void ProcessClientMessages()
    {
        while (_state.IsConnected)
        {
            int received = _clientSocket.Receive(_buffer);

            if (received == 0)
            {
                _state.MarkAsDisconnectedNaturally();
            }
            else
            {
                ProcessReceivedMessage(received);
            }
        }
    }

    /// <summary>
    /// Processes a received message
    /// </summary>
    /// <param name="received">Number of bytes received</param>
    private void ProcessReceivedMessage(int received)
    {
        string message = Encoding.UTF8.GetString(_buffer, 0, received);
        Console.WriteLine($"Cliente dice: {message}");
        EchoMessage(message);
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
    /// Sends an echo message back to the client
    /// </summary>
    /// <param name="message">The message to echo</param>
    private void EchoMessage(string message)
    {
        try
        {
            string echoMessage = $"Echo: {message}";
            byte[] data = Encoding.UTF8.GetBytes(echoMessage);
            _clientSocket.Send(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando mensaje echo: {ex.Message}");
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