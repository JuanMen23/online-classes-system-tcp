using System.Net;
using System.Net.Sockets;
using Common.Config;
using Server.Services;

namespace Server;

/// <summary>
/// Main server application class that handles socket connections and client management
/// </summary>
public class ServerApplication
{
    private readonly AppConfig _config;
    private Socket? _serverSocket;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the ServerApplication
    /// </summary>
    /// <param name="config">Application configuration</param>
    public ServerApplication(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Starts the server and begins listening for client connections
    /// </summary>
    public void Start()
    {
        try
        {
            InitializeServer();
            _isRunning = true;
            
            Console.WriteLine($"Server listening on {_config.ServerIp}:{_config.ServerPort}");
            Console.WriteLine("Waiting for clients...");

            // Main server loop
            while (_isRunning)
            {
                try
                {
                    // Accept incoming connection (blocking until a client connects)
                    Socket clientSocket = _serverSocket!.Accept();
                    Console.WriteLine("Client connected");

                    // Handle each client in a separate thread
                    var clientHandler = new ClientHandler(clientSocket);
                    new Thread(() => clientHandler.HandleClient()).Start();
                }
                catch (ObjectDisposedException)
                {
                    // Server socket was closed, exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"Error accepting client connection: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server startup error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the server gracefully
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        
        try
        {
            _serverSocket?.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // Ignore shutdown errors
        }
        finally
        {
            _serverSocket?.Close();
        }
        
        Console.WriteLine("Server stopped");
    }

    /// <summary>
    /// Initializes the server socket
    /// </summary>
    private void InitializeServer()
    {
        // Create server socket: IPv4 + TCP (connection-oriented)
        _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Define local endpoint (IP and port where it will listen)
        var localEndpoint = _config.GetServerEndPoint();

        // Bind and start listening
        _serverSocket.Bind(localEndpoint);
        _serverSocket.Listen(_config.MaxBacklogConnections);
    }

}
