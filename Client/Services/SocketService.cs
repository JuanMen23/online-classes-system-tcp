using System.Threading.Tasks;
using System.Net.Sockets;
using Common.Config;
using Common.Protocol;

namespace Client.Services;

/// <summary>
/// Service class responsible for protocol-based socket communication with the server
/// </summary>
public class SocketService
{
    private readonly AppConfig _config;
    private Socket? _clientSocket;
    private readonly ProtocolHandler _protocolHandler;

    /// <summary>
    /// Initializes a new instance of the SocketService
    /// </summary>
    /// <param name="config">Application configuration</param>
    public SocketService(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _protocolHandler = new ProtocolHandler();
    }

    /// <summary>
    /// Connects to the server
    /// </summary>
    public void Connect()
    {
        if (IsConnected)
        {
            Console.WriteLine("Socket already connected, attempting to disconnect first...");
            Disconnect();
        }
        try
        {
            // Create a TCP socket (IPv4)
            _clientSocket = new Socket(
                AddressFamily.InterNetwork,   // IPv4
                SocketType.Stream,            // Connection-oriented socket
                ProtocolType.Tcp              // TCP protocol
            );

            // Define local endpoint (port 0 = OS assigns a free port automatically)
            var localEndpoint = _config.GetLocalEndPoint();

            // Define remote endpoint (the server)
            var remoteEndpoint = _config.GetServerEndPoint();

            // Associate socket to local endpoint
            _clientSocket.Bind(localEndpoint);

            // Connect to server
            _clientSocket.Connect(remoteEndpoint);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to connect to server: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets whether the client is connected to the server
    /// </summary>
    public bool IsConnected => _clientSocket?.Connected ?? false;
    
    /// <summary>
    /// Sends a protocol message to the server
    /// </summary>
    /// <param name="message">The protocol message to send</param>
    public async Task SendMessageAsync(ProtocolMessage message)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected to server");
        await _protocolHandler.SendMessageAsync(_clientSocket!, message);
    }
    
    /// <summary>
    /// Receives a protocol message from the server
    /// </summary>
    /// <returns>The received protocol message, or null if connection was closed</returns>
    public async Task<ProtocolMessage?> ReceiveMessageAsync()
    {
        if (!IsConnected) return null;
        try
        {
            return await _protocolHandler.ReceiveMessageAsync(_clientSocket!);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("closed"))
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }
    
    public Socket GetSocket()
    {
        if (_clientSocket == null) throw new InvalidOperationException("Socket not initialized.");
        return _clientSocket;
    }


    /// <summary>
    /// Disconnects from the server
    /// </summary>
    public void Disconnect()
    {
        if (_clientSocket == null) return;
        
        try
        {
            if (_clientSocket.Connected)
            {
                // Close connection gracefully
                _clientSocket.Shutdown(SocketShutdown.Both);  // Close send and receive
            }
        }
        catch
        {
            // Ignore shutdown errors
        }
        finally
        {
            _clientSocket?.Close();
            _clientSocket = null;
        }
    }
}
