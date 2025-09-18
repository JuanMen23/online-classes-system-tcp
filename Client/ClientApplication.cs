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
    
    private volatile bool _waitingForServerResponse = false;
    
    // For user's session handling
    private bool _isLoggedIn = false;
    private string? _currentUser = null;

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
                try
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
                    case "3":
                        RequestClassList();
                        break;

                    case "0":
                        _isRunning = false;
                        break;

                    default:
                        Console.WriteLine("Opción inválida");
                        break;
                    if (_waitingForServerResponse)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    Console.WriteLine();
                    
                    if (!_isLoggedIn)
                    {
                        ShowLoggedOutMenu();
                    }
                    else
                    {
                        ShowLoggedInMenu();
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

    private void ShowMenu()
    {
        Console.WriteLine();
        Console.WriteLine("===== MENÚ CLIENTE =====");
        Console.WriteLine("1. Crear clase");
        Console.WriteLine("2. Enviar mensaje (prueba)");
        Console.WriteLine("3. Ver clases disponibles");
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

        // Cupos
        int maxSeats;
        while (true)
        {
            Console.Write("Cupos máximos: ");
            string input = Console.ReadLine() ?? "";
            if (int.TryParse(input, out maxSeats) && maxSeats > 0)
                break;
            Console.WriteLine("⚠️ Ingrese un número válido mayor a 0.");
        }

        // Duración
        int duration;
        while (true)
        {
            Console.Write("Duración (minutos): ");
            string input = Console.ReadLine() ?? "";
            if (int.TryParse(input, out duration) && duration > 0)
                break;
            Console.WriteLine("⚠️ Ingrese un número válido mayor a 0.");
        }

        // Fecha
        DateTime startDateTime;
        while (true)
        {
            Console.Write("Fecha y hora (yyyy-MM-dd HH:mm) o vacío para ahora: ");
            string input = Console.ReadLine() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                startDateTime = DateTime.Now;
                break;
            }
            if (DateTime.TryParse(input, out startDateTime))
                break;

            Console.WriteLine("⚠️ Formato de fecha inválido. Ejemplo: 2025-09-15 14:30");
        }

        // Imagen
        Console.Write("Ruta imagen (opcional): ");
        string imagePath = Console.ReadLine() ?? "";
        string imageBase64 = "";
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(imagePath);

                if (bytes.Length > 5 * 1024 * 1024)
                {
                    Console.WriteLine("⚠️ Imagen demasiado grande (máximo 5MB). No se enviará.");
                }
                else
                {
                    imageBase64 = Convert.ToBase64String(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error leyendo la imagen: {ex.Message}");
            }
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
    
    private void RequestClassList()
    {
        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_LIST_CLASSES,
            "" // no necesitamos data
        );

        _socketService.SendMessage(request);
        Console.WriteLine("Solicitud de listado de clases enviada.");
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
                ProtocolMessage? response = _socketService.ReceiveMessage();
                
                if (response == null)
                {
                    Console.WriteLine("Servidor desconectado");
                    _isRunning = false;
                    break;
                }
                if (message.Command == ProtocolConstants.CMD_LIST_CLASSES)
                {
                    Console.WriteLine("===== Clases disponibles =====");
                    Console.WriteLine(message.Data);
                }
                else if (message.Command == ProtocolConstants.CMD_ERROR)
                {
                    Console.WriteLine($"⚠️ Error: {message.Data}");
                }
                else
                {
                    Console.WriteLine($"[Servidor] {message.Data}");
                
                switch(response.Command)
                {
                    case ProtocolConstants.CMD_REGISTER:
                        if(response.Data == ProtocolConstants.RESPONSE_OK)
                            Console.WriteLine("\n-> ¡Registro exitoso! Ahora puedes iniciar sesión.");
                        else
                            Console.WriteLine($"\n-> Error de registro: {response.Data}");
                        break;

                    case ProtocolConstants.CMD_LOGIN:
                        if(response.Data == ProtocolConstants.RESPONSE_OK)
                        {
                            _isLoggedIn = true;
                            Console.WriteLine($"\n-> ¡Bienvenido, {_currentUser}!");
                        }
                        else 
                        {
                            _currentUser = null;
                            Console.WriteLine($"\n-> Error de inicio de sesión: {response.Data}");
                        }
                        break;

                    case ProtocolConstants.CMD_LOGOUT:
                        _isLoggedIn = false;
                        _currentUser = null;
                        Console.WriteLine("\n-> Sesión cerrada correctamente.");
                        break;
                    
                    default:
                        Console.WriteLine($"Respuesta del servidor (CMD {response.Command}): {response.Data}");
                        break;
                }
                
                _waitingForServerResponse = false;
                if (_isRunning)
                {
                    Console.Write("\n> ");
                }
            }
        }
        catch (Exception ex)
        {
            if (_isRunning)
            {
                Console.WriteLine("\nSe perdió la conexión con el servidor. Presione ENTER para salir.");
                _isRunning = false; // Stop the application
            }
        }
    }
    
    /// <summary>
    /// Menu when no user is logged
    /// </summary>
    private void ShowLoggedOutMenu()
    {
        Console.WriteLine("--- Menú ---");
        Console.WriteLine("1. Registrarse");
        Console.WriteLine("2. Iniciar Sesión");
        Console.WriteLine("3. Salir");
        Console.Write("> ");
        string? choice = Console.ReadLine();

        switch (choice?.ToLower())
        {
            case "1": HandleRegister(); break;
            case "2": HandleLogin(); break;
            case "3": _isRunning = false; break;
            default: Console.WriteLine("Opción no válida."); break;
        }
    }

    /// <summary>
    /// Menu for logged in user
    /// </summary>
    private void ShowLoggedInMenu()
    {
        Console.WriteLine($"--- Conectado como: {_currentUser} ---");
        Console.WriteLine("1. Crear clase");
        Console.WriteLine("2. Cerrar Sesión (Logout)");
        Console.Write("\n > Seleccione una opción: ");
        
        string? choice = Console.ReadLine();
        if (string.IsNullOrEmpty(choice)) return; 

        switch (choice)
        {
            case "1":
                CreateClass();
                break;

            case "2":
                HandleLogout();
                break;

            default:
                Console.WriteLine("Opción no válida.");
                break;
        }
    }

    /// <summary>
    /// Handles user registration
    /// </summary>
    private void HandleRegister()
    {
        Console.Write("Ingrese nombre de usuario: ");
        string? username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        string? password = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("El usuario y la contraseña no pueden estar vacíos.");
            return;
        }

        string data = $"{username}|{password}";
        var message = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST, 
            ProtocolConstants.CMD_REGISTER, 
            data
            );
        
        _waitingForServerResponse = true;
        _socketService.SendMessage(message);
        Console.WriteLine("Enviando datos de registro...");
    }

    /// <summary>
    /// Data's Login
    /// </summary>
    private void HandleLogin()
    {
        Console.Write("Ingrese nombre de usuario: ");
        var username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        var password = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("El usuario y la contraseña no pueden estar vacíos.");
            return;
        }
        
        _currentUser = username;
        var data = $"{username}|{password}";
        var message = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST, 
            ProtocolConstants.CMD_LOGIN, 
            data
            );
        
        _waitingForServerResponse = true; 
        _socketService.SendMessage(message);
        Console.WriteLine("Iniciando sesión...");
    }

    /// <summary>
    /// Data logged out
    /// </summary>
    private void HandleLogout()
    {
        var message = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST, 
            ProtocolConstants.CMD_LOGOUT, 
            ""
            );
        
        _waitingForServerResponse = true;
        _socketService.SendMessage(message);
    }


    private void Disconnect()
    {
        Console.WriteLine("Desconectando...");
        _socketService.Disconnect();
        Console.WriteLine("Desconectado");
    }
}
