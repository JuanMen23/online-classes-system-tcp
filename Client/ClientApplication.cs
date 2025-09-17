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

            // Thread para recibir mensajes del servidor
            var receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            // Loop principal con menú
            while (_isRunning)
            {
                MostrarMenu();

                string? opcion = Console.ReadLine();
                if (string.IsNullOrEmpty(opcion))
                    continue;

                switch (opcion)
                {
                    case "1":
                        CrearClase();
                        break;

                    case "2":
                        // Provisorio: mandar mensaje libre (para testear)
                        Console.Write("Mensaje: ");
                        string? message = Console.ReadLine();
                        if (!string.IsNullOrEmpty(message))
                        {
                            var protocolMessage = new ProtocolMessage(
                                ProtocolConstants.HEADER_REQUEST,
                                ProtocolConstants.CMD_LOGIN, // Provisorio
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

    private void MostrarMenu()
    {
        Console.WriteLine();
        Console.WriteLine("===== MENÚ CLIENTE =====");
        Console.WriteLine("1. Crear clase");
        Console.WriteLine("2. Enviar mensaje (prueba)");
        Console.WriteLine("0. Salir");
        Console.Write("Seleccione una opción: ");
    }

    /// <summary>
    /// Solicita datos al usuario y envía un request CMD_CREATE_CLASS
    /// </summary>
    private void CrearClase()
    {
        Console.Write("Nombre: ");
        string nombre = Console.ReadLine() ?? "";

        Console.Write("Descripción: ");
        string descripcion = Console.ReadLine() ?? "";

        Console.Write("Cupos máximos: ");
        int cupos = int.TryParse(Console.ReadLine(), out var c) ? c : 0;

        Console.Write("Duración (minutos): ");
        int duracion = int.TryParse(Console.ReadLine(), out var d) ? d : 0;

        Console.Write("Fecha y hora (yyyy-MM-dd HH:mm) o vacío para ahora: ");
        string inputFecha = Console.ReadLine() ?? "";
        DateTime fecha = string.IsNullOrEmpty(inputFecha) ? DateTime.Now : DateTime.Parse(inputFecha);

        Console.Write("Ruta imagen (opcional): ");
        string rutaImagen = Console.ReadLine() ?? "";
        string imagenBase64 = "";
        if (!string.IsNullOrEmpty(rutaImagen) && File.Exists(rutaImagen))
        {
            imagenBase64 = Convert.ToBase64String(File.ReadAllBytes(rutaImagen));
        }

        string datos = $"{nombre}|{descripcion}|{cupos}|{fecha:yyyy-MM-dd HH:mm}|{duracion}|{imagenBase64}";

        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_CREATE_CLASS,  
            datos
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