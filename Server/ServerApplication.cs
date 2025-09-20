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
    private readonly ClientManager _clientManager;

    /// <summary>
    /// Initializes a new instance of the ServerApplication
    /// </summary>
    /// <param name="config">Application configuration</param>
    public ServerApplication(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _clientManager = ClientManager.Instance;
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
            
            Console.WriteLine($"Servidor escuchando en {_config.ServerIp}:{_config.ServerPort}");
            Console.WriteLine("Esperando clientes...");
            Console.WriteLine($"Escriba '{Common.Protocol.ProtocolConstants.EXIT_COMMAND}' para cerrar el servidor de forma controlada");

            // Start console command handler in a separate thread
            var consoleThread = new Thread(HandleConsoleCommands);
            consoleThread.IsBackground = true;
            consoleThread.Start();

            // Main server loop
            while (_isRunning)
            {
                try
                {
                    // Accept incoming connection (blocking until a client connects)
                    Socket clientSocket = _serverSocket!.Accept();
                    Console.WriteLine("Cliente conectado");

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
                        Console.WriteLine($"Error aceptando conexión de cliente: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar el servidor: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the server gracefully
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return; // Already stopped
        }
        
        _isRunning = false;
        
        // Disconnect all clients first
        _clientManager.DisconnectAllClients();
        
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
        
        Console.WriteLine("Servidor cerrado");
    }

    /// <summary>
    /// Handles console commands for server control
    /// </summary>
    private void HandleConsoleCommands()
    {
        while (_isRunning)
        {
            try
            {
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                
                string command = input.Trim().ToUpper();
                
                switch (command)
                {
                    case Common.Protocol.ProtocolConstants.EXIT_COMMAND:
                        Console.WriteLine("Iniciando cierre controlado del servidor...");
                        Stop();
                        return;
                        
                    default:
                        Console.WriteLine($"Comando desconocido: '{input}'. Escriba '{Common.Protocol.ProtocolConstants.EXIT_COMMAND}' para cerrar el servidor.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando comando: {ex.Message}");
            }
        }
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