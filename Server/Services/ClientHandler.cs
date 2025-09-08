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

    /// <summary>
    /// Initializes a new instance of the ClientHandler
    /// </summary>
    /// <param name="clientSocket">The client socket to handle</param>
    public ClientHandler(Socket clientSocket)
    {
        _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
        _buffer = new byte[Common.Protocol.ProtocolConstants.MAX_BUFFER_SIZE];
    }

    /// <summary>
    /// Handles the client connection and processes incoming messages
    /// </summary>
    public void HandleClient()
    {
        bool clientIsConnected = true;

        try
        {
            while (clientIsConnected)
            {
                // Receive data from client
                int received = _clientSocket.Receive(_buffer);

                // received == 0 means the client closed the connection gracefully
                if (received == 0)
                {
                    clientIsConnected = false;
                }
                else
                {
                    // Decode only the valid bytes [0..received)
                    string message = Encoding.UTF8.GetString(_buffer, 0, received);
                    Console.WriteLine($"Client says: {message}");

                    // Echo the message back to the client
                    EchoMessage(message);
                }
            }
        }
        catch (SocketException ex)
        {
            // Typical exception if client disconnects abruptly
            Console.WriteLine($"Client disconnected abruptly: {ex.Message}");
        }
        catch (ObjectDisposedException)
        {
            // Socket was already closed from elsewhere
            Console.WriteLine("Client socket was disposed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
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
            Console.WriteLine($"Error sending echo message: {ex.Message}");
        }
    }

    /// <summary>
    /// Disconnects the client cleanly
    /// </summary>
    private void DisconnectClient()
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

        Console.WriteLine("Client disconnected");
    }
}