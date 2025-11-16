using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Common;
using Common.Config;
using Server.GrpcServices;
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
    private readonly GrpcServerHost _grpcServerHost;

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
        _grpcServerHost = new GrpcServerHost(config);

    }

    /// <summary>
    /// Starts the server and begins listening for client connections
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            InitializeServer();
            await _grpcServerHost.StartAsync();
            _isRunning = true;
            
            Console.WriteLine($"Servidor escuchando en {_config.ServerIp}:{_config.ServerPort}");
            Console.WriteLine("Esperando clientes...");
            Console.WriteLine($"Escriba '{Common.Protocol.ProtocolConstants.EXIT_COMMAND}' para cerrar el servidor de forma controlada");
            LoggingService.Instance.PublishLog(
                "server_start",
                "system",
                $"Servidor escuchando en {_config.ServerIp}:{_config.ServerPort}",
                metadata: new Dictionary<string, string>
                {
                    ["ip"] = _config.ServerIp,
                    ["port"] = _config.ServerPort.ToString(),
                    ["backlog"] = _config.MaxBacklogConnections.ToString()
                });

            // Start console command handler in a separate task
            _ = Task.Run(HandleConsoleCommands);

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
                        LoggingService.Instance.PublishLog(
                            "client_accept_error",
                            "system",
                            ex.Message,
                            "ERROR");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al iniciar el servidor: {ex.Message}");
            LoggingService.Instance.PublishLog(
                "server_start_error",
                "system",
                ex.Message,
                "ERROR");
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
            _grpcServerHost.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deteniendo el servidor gRPC: {ex.Message}");
        }
        
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
        LoggingService.Instance.PublishLog(
            "server_stop",
            "system",
            "Servidor detenido correctamente");
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
                        LoggingService.Instance.PublishLog(
                            "console_exit",
                            "system",
                            "Se recibió el comando EXIT");
                        Stop();
                        return;

                    case "REPORT":
                        Console.WriteLine("Generando reporte del día... (presione C para cancelar)");

                        LoggingService.Instance.PublishLog(
                            "report_requested",
                            "system",
                            "Solicitud de reporte manual recibida");

                        var reportTask = _reportService.GenerateReportAsync(_reportCancellationToken.Token);

                        while (!reportTask.IsCompleted)
                        {
                            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
                            {
                                _reportCancellationToken.Cancel();
                                Console.WriteLine("Cancelando reporte...");
                                LoggingService.Instance.PublishLog(
                                    "report_cancelled",
                                    "system",
                                    "Reporte cancelado por el operador");
                                break;
                            }
                            Thread.Sleep(200);
                        }

                        if (!_reportCancellationToken.IsCancellationRequested)
                        {
                            var reportResult = await reportTask;
                            Console.WriteLine(reportResult);
                            LoggingService.Instance.PublishLog(
                                "report_generated",
                                "system",
                                "Reporte generado correctamente",
                                metadata: new Dictionary<string, string>
                                {
                                    ["summary"] = reportResult.Replace(Environment.NewLine, " | ")
                                });
                        }
                        else
                        {
                            Console.WriteLine("Reporte cancelado.");
                            _reportCancellationToken = new CancellationTokenSource(); // reset para la próxima vez
                        }

                        break;

                    default:
                        Console.WriteLine($"Comando desconocido: '{input}'. Escriba 'EXIT' o 'REPORT'.");
                        LoggingService.Instance.PublishLog(
                            "console_unknown_command",
                            "system",
                            $"Comando desconocido recibido: {input}",
                            "WARN");
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando comando: {ex.Message}");
                LoggingService.Instance.PublishLog(
                    "console_command_error",
                    "system",
                    ex.Message,
                    "ERROR");
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
            LoggingService.Instance.PublishLog(
                "data_file_missing",
                "system",
                $"No se encontró el archivo de datos en {filePath}",
                "WARN");
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
                    
                    UserService.Instance.RegisterUser(username, password, null, emitLog: false); 
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
            LoggingService.Instance.PublishLog(
                "data_loaded",
                "system",
                $"Datos cargados desde {filePath}",
                metadata: new Dictionary<string, string>
                {
                    ["users"] = usersLoaded.ToString(),
                    ["classes"] = classesLoaded.ToString()
                });
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al cargar datos desde {filePath}: {ex.Message}");
            LoggingService.Instance.PublishLog(
                "data_load_error",
                "system",
                ex.Message,
                "ERROR");
        }
    }
}