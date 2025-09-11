using Common.Config;
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
            Console.WriteLine("Starting Client Application...");
            Console.WriteLine("Connecting to server...");

            // Connect to server
            _socketService.Connect();

            Console.WriteLine("Connected to server!");
            Console.WriteLine("Type a message and press ENTER to send it");
            Console.WriteLine($"Type '{Common.Protocol.ProtocolConstants.EXIT_COMMAND}' to exit");

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
                        // Send message to server
                        _socketService.SendMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading input: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client startup error: {ex.Message}");
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
                string? message = _socketService.ReceiveMessage();
                
                if (message != null)
                {
                    Console.WriteLine($"Server response: {message}");
                }
                else
                {
                    // Server disconnected
                    Console.WriteLine("Server disconnected");
                    _isRunning = false;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine($"Error receiving messages: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Disconnects from the server
    /// </summary>
    private void Disconnect()
    {
        Console.WriteLine("Disconnecting...");
        _socketService.Disconnect();
        Console.WriteLine("Disconnected");
    }
}
