using Common.Config;
using Common.Protocol;
using Client.Services;

namespace Client;

/// <summary>
/// Main client application class that handles user interaction and server communication
/// </summary>
public class ClientApplication
{
    private readonly AppConfig _config;
    private readonly SocketService _socketService;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the ClientApplication
    /// </summary>
    /// <param name="config">Application configuration</param>
    public ClientApplication(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _socketService = new SocketService(_config);
    }

    /// <summary>
    /// Starts the client application and begins user interaction
    /// </summary>
    public void Start()
    {
        try
        {
            Console.WriteLine("Iniciando aplicación del cliente...");
            Console.WriteLine("Conectando al servidor...");

            // Connect to server
            _socketService.Connect();

            Console.WriteLine("¡Conectado al servidor!");
            Console.WriteLine("Escriba un mensaje y presione ENTER para enviarlo");
            Console.WriteLine($"Escriba '{Common.Protocol.ProtocolConstants.EXIT_COMMAND}' para salir");

            _isRunning = true;

            // Start receiving messages in a separate thread
            var receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Main user input loop
            while (_isRunning)
            {
                try
                {
                    string? message = Console.ReadLine();

                    if (string.IsNullOrEmpty(message))
                        continue;

                    if (message.Equals(Common.Protocol.ProtocolConstants.EXIT_COMMAND, StringComparison.OrdinalIgnoreCase))
                    {
                        _isRunning = false;
                    }
                    else
                    {
                        // Send protocol message to server
                        var protocolMessage = new ProtocolMessage(
                            ProtocolConstants.HEADER_REQUEST,
                            ProtocolConstants.CMD_LOGIN, // For now, use LOGIN command
                            message
                        );
                        _socketService.SendMessage(protocolMessage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error leyendo entrada: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar el cliente: {ex.Message}");
        }
        finally
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Receives messages from the server in a separate thread
    /// </summary>
    private void ReceiveMessages()
    {
        try
        {
            while (_isRunning)
            {
                ProtocolMessage? message = _socketService.ReceiveMessage();
                
                if (message != null)
                {
                    Console.WriteLine($"Respuesta del servidor: {message.Data}");
                }
                else
                {
                    // Server disconnected
                    Console.WriteLine("Servidor desconectado");
                    _isRunning = false;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine($"Servidor desconectado: {ex.Message}");
                Console.WriteLine("Presione ENTER para salir...");
                _isRunning = false; // Stop the application
            }
        }
    }

    /// <summary>
    /// Disconnects from the server
    /// </summary>
    private void Disconnect()
    {
        Console.WriteLine("Desconectando...");
        _socketService.Disconnect();
        Console.WriteLine("Desconectado");
    }
}
