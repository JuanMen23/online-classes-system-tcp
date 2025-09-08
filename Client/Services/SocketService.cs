using System.Net;
using System.Net.Sockets;
using System.Text;
using Common.Config;

namespace Client.Services;

/// <summary>
/// Service class responsible for socket communication with the server
/// </summary>
public class SocketService
{
    private readonly AppConfig _config;
    private Socket? _clientSocket;
    private readonly byte[] _buffer;

    /// <summary>
    /// Initializes a new instance of the SocketService
    /// </summary>
    /// <param name="config">Application configuration</param>
    public SocketService(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _buffer = new byte[_config.BufferSize];
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
    /// Sends a message to the server
    /// </summary>
    /// <param name="message">The message to send</param>
    public void SendMessage(string message)
    {
        if (_clientSocket == null || !_clientSocket.Connected)
        {
            throw new InvalidOperationException("Not connected to server");
        }

        try
        {
            // Convert message to bytes before sending
            byte[] data = Encoding.UTF8.GetBytes(message);
            _clientSocket.Send(data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Receives a message from the server
    /// </summary>
    /// <returns>The received message, or null if connection was closed</returns>
    public string? ReceiveMessage()
    {
        if (_clientSocket == null || !_clientSocket.Connected)
        {
            return null;
        }

        try
        {
            // Receive data from server
            int received = _clientSocket.Receive(_buffer);

            // received == 0 means the server closed the connection gracefully
            if (received == 0)
            {
                return null;
            }

            // Decode only the valid bytes [0..received)
            return Encoding.UTF8.GetString(_buffer, 0, received);
        }
        catch (SocketException)
        {
            // Server disconnected abruptly
            return null;
        }
        catch (ObjectDisposedException)
        {
            // Socket was already closed
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to receive message: {ex.Message}", ex);
        }
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
