using System.Net.Sockets;
using Common;
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
    private readonly DailyReportService _reportService;
    private CancellationTokenSource _reportCancellationToken = new();

    /// <summary>
    /// Initializes a new instance of the ServerApplication
    /// </summary>
    /// <param name="config">Application configuration</param>
    public ServerApplication(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        LoadDataFromFile();
        _clientManager = ClientManager.Instance;
        _reportService = new DailyReportService(ClassService.Instance, config.ServerImageDirectory);

    }

    /// <summary>
    /// Starts the server and begins listening for client connections
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            InitializeServer();
            _isRunning = true;
            
            Console.WriteLine($"Servidor escuchando en {_config.ServerIp}:{_config.ServerPort}");
            Console.WriteLine("Esperando clientes...");
            Console.WriteLine($"Escriba '{Common.Protocol.ProtocolConstants.EXIT_COMMAND}' para cerrar el servidor de forma controlada");

            // StartAsync console command handler in a separate thread
            var consoleThread = new Thread(HandleConsoleCommands)
            {
                IsBackground = true
            };
            consoleThread.Start();

            // Main server loop
            while (_isRunning)
            {
                try
                {
                    // Accept incoming connection (blocking until a client connects)
                    Socket clientSocket = await _serverSocket!.AcceptAsync();
                    Console.WriteLine("Cliente conectado");

                    // Handle each client in a separate thread
                    var clientHandler = new ClientHandler(clientSocket);
                    _ = clientHandler.HandleClientAsync();
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
        finally { Stop();}
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
    private async void HandleConsoleCommands()
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

                    case "REPORT":
                        Console.WriteLine("Generando reporte del día... (presione C para cancelar)");

                        var reportTask = _reportService.GenerateReportAsync(_reportCancellationToken.Token);

                        while (!reportTask.IsCompleted)
                        {
                            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
                            {
                                _reportCancellationToken.Cancel();
                                Console.WriteLine("Cancelando reporte...");
                                break;
                            }
                            Thread.Sleep(200);
                        }

                        if (!_reportCancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine(await reportTask);
                        }
                        else
                        {
                            Console.WriteLine("Reporte cancelado.");
                            _reportCancellationToken = new CancellationTokenSource(); // reset para la próxima vez
                        }

                        break;

                    default:
                        Console.WriteLine($"Comando desconocido: '{input}'. Escriba 'EXIT' o 'REPORT'.");
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
    
    private void LoadDataFromFile(string fileName = "database.txt")
    {
        string exePath = AppContext.BaseDirectory;
        string filePath = Path.Combine(exePath, fileName);
        
        if (!File.Exists(filePath))
        {
            PrintMessage.Information($"Advertencia: No se encontró el archivo de datos ({filePath}). El servidor iniciará vacío.");
            return;
        }

        try
        {
            var lines = File.ReadAllLines(filePath);
            int usersLoaded = 0;
            int classesLoaded = 0;
            int maxClassId = 0;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('|');
                if (parts.Length < 2) continue;

                var type = parts[0];

                if (type == "USER" && parts.Length == 3)
                {
                    var username = parts[1];
                    var password = parts[2];
                    
                    UserService.Instance.RegisterUser(username, password); 
                    usersLoaded++;
                }
                else if (type == "CLASS" && parts.Length >= 9)
                {
                    var classId = int.Parse(parts[1]);
                    var name = parts[2];
                    var description = parts[3];
                    var maxSeats = int.Parse(parts[4]);
                    var startDateTime = DateTime.Parse(parts[5]);
                    var durationMinutes = int.Parse(parts[6]);
                    var imagePath = string.IsNullOrEmpty(parts[7]) ? null : parts[7];
                    var createdBy = parts[8];
                    
                    var enrolledUsers = new List<string>();
                    if (parts.Length > 9 && !string.IsNullOrEmpty(parts[9]))
                    {
                        enrolledUsers = parts[9].Split(',').Select(u => u.Trim()).ToList();
                    }

                    ClassService.Instance.CreateClassFromData(classId, name, description, maxSeats, startDateTime, durationMinutes, imagePath, createdBy, enrolledUsers);
                    if (classId > maxClassId) maxClassId = classId;
                    classesLoaded++;
                }
            }

            ClassService.Instance.SetNextId(maxClassId + 1);
            PrintMessage.Success($"Datos cargados desde {filePath}: {usersLoaded} usuarios y {classesLoaded} clases.");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al cargar datos desde {filePath}: {ex.Message}");
        }
    }
}