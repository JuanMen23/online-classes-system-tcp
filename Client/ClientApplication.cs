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

    public ClientApplication(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _socketService = new SocketService(_config);
    }

    public void Start()
    {
        try
        {
            Console.WriteLine("Iniciando aplicación del cliente...");
            Console.WriteLine("Conectando al servidor...");

            _socketService.Connect();

            Console.WriteLine("¡Conectado al servidor!");
            _isRunning = true;

            // Start thread to receive messages from server
            var receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Main loop with menu
            while (_isRunning)
            {
                ShowMenu();

                string? option = Console.ReadLine();
                if (string.IsNullOrEmpty(option))
                    continue;

                switch (option)
                {
                    case "1":
                        CreateClass();
                        break;

                    case "2":
                        // Temporary: send free message (for testing)
                        Console.Write("Mensaje: ");
                        string? message = Console.ReadLine();
                        if (!string.IsNullOrEmpty(message))
                        {
                            var protocolMessage = new ProtocolMessage(
                                ProtocolConstants.HEADER_REQUEST,
                                ProtocolConstants.CMD_LOGIN, // Temporary
                                message
                            );
                            _socketService.SendMessage(protocolMessage);
                        }
                        break;

                    case "0":
                        _isRunning = false;
                        break;

                    default:
                        Console.WriteLine("Opción inválida");
                        break;
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

    private void ShowMenu()
    {
        Console.WriteLine();
        Console.WriteLine("===== MENÚ CLIENTE =====");
        Console.WriteLine("1. Crear clase");
        Console.WriteLine("2. Enviar mensaje (prueba)");
        Console.WriteLine("0. Salir");
        Console.Write("Seleccione una opción: ");
    }

    /// <summary>
    /// Asks user for class data and sends CMD_CREATE_CLASS request
    /// </summary>
    private void CreateClass()
    {
        Console.Write("Nombre: ");
        string name = Console.ReadLine() ?? "";

        Console.Write("Descripción: ");
        string description = Console.ReadLine() ?? "";

        Console.Write("Cupos máximos: ");
        int maxSeats = int.TryParse(Console.ReadLine(), out var c) ? c : 0;

        Console.Write("Duración (minutos): ");
        int duration = int.TryParse(Console.ReadLine(), out var d) ? d : 0;

        Console.Write("Fecha y hora (yyyy-MM-dd HH:mm) o vacío para ahora: ");
        string inputDate = Console.ReadLine() ?? "";
        DateTime startDateTime = string.IsNullOrEmpty(inputDate) ? DateTime.Now : DateTime.Parse(inputDate);

        Console.Write("Ruta imagen (opcional): ");
        string imagePath = Console.ReadLine() ?? "";
        string imageBase64 = "";
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            imageBase64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));
        }

        string data = $"{name}|{description}|{maxSeats}|{startDateTime:yyyy-MM-dd HH:mm}|{duration}|{imageBase64}";

        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_CREATE_CLASS,
            data
        );

        _socketService.SendMessage(request);
        Console.WriteLine("Solicitud de creación de clase enviada.");
    }

    private void ReceiveMessages()
    {
        try
        {
            while (_isRunning)
            {
                ProtocolMessage? message = _socketService.ReceiveMessage();

                if (message != null)
                {
                    Console.WriteLine($"[Servidor] {message.Data}");
                }
                else
                {
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
                _isRunning = false;
            }
        }
    }

    private void Disconnect()
    {
        Console.WriteLine("Desconectando...");
        _socketService.Disconnect();
        Console.WriteLine("Desconectado");
    }
}
