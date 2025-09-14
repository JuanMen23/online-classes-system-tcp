using System.Net;
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
    public void SendMessage(ProtocolMessage message)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected to server");
        _protocolHandler.SendMessage(_clientSocket!, message);
    }
    
    /// <summary>
    /// Receives a protocol message from the server
    /// </summary>
    /// <returns>The received protocol message, or null if connection was closed</returns>
    public ProtocolMessage? ReceiveMessage()
    {
        if (!IsConnected) return null;
        return _protocolHandler.ReceiveMessage(_clientSocket!);
    }


    /// <summary>
    /// Disconnects from the server
    /// </summary>
    public void Disconnect()
    {
        try
        {
            if (_clientSocket != null && _clientSocket.Connected)
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
            _clientSocket?.Close();  // Release resources
        }
    }
}
