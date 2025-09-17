using System.Net.Sockets;
using System.Text;
using Common.Protocol;
using Server.ClassSession; 
using Server.Services; 

namespace Server.Services;

/// <summary>
/// Handles individual client connections and communication using the protocol
/// </summary>
public class ClientHandler
{
    private readonly Socket _clientSocket;
    private readonly ClientManager _clientManager;
    private readonly ClientState _state;
    private readonly ProtocolHandler _protocolHandler;
    private readonly ClassManager _classManager;
    
    public Guid Id { get; }

    /// <summary>
    /// Initializes a new instance of the ClientHandler
    /// </summary>
    /// <param name="clientSocket">The client socket to handle</param>
    public ClientHandler(Socket clientSocket)
    {
        _clientSocket = clientSocket ?? throw new ArgumentNullException(nameof(clientSocket));
        _clientManager = ClientManager.Instance;
        _state = new ClientState();
        _protocolHandler = new ProtocolHandler();
        _classManager = ClassManager.Instance;
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Handles the client connection and processes incoming messages
    /// </summary>
    public void HandleClient()
    {
        RegisterClient();
        
        try
        {
            ProcessClientMessages();
        }
        catch (SocketException ex)
        {
            HandleSocketException(ex);
        }
        catch (ObjectDisposedException)
        {
            HandleObjectDisposedException();
        }
        catch (Exception ex)
        {
            HandleGenericException(ex);
        }
        finally
        {
            CleanupClient();
        }
    }

    /// <summary>
    /// Registers the client with the manager
    /// </summary>
    private void RegisterClient()
    {
        _clientManager.AddClient(this);
    }

    /// <summary>
    /// Processes incoming protocol messages from the client
    /// </summary>
    private void ProcessClientMessages()
    {
        while (_state.IsConnected)
        {
            try
            {
                // Receive protocol message from client
                var receivedMessage = _protocolHandler.ReceiveMessage(_clientSocket);
                Console.WriteLine($"Recibido: {receivedMessage}");
                
                // Process the command (for now, just echo back)
                ProcessCommand(receivedMessage);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Connection closed"))
            {
                // Client disconnected gracefully
                _state.MarkAsDisconnectedNaturally();
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionAborted)
            {
                Console.WriteLine("Servidor se cerró - desconectando cliente");
                _state.MarkAsDisconnectedNaturally();
            }
            catch (Exception ex) when (ex.Message.Contains("Software caused connection abort"))
            {
                // Conexión abortada por cierre del servidor - esto es esperado, no es un error
                _state.MarkAsDisconnectedNaturally();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recibiendo mensaje del protocolo: {ex.Message}");
                _state.MarkAsDisconnectedNaturally();
            }
        }
    }

    /// <summary>
    /// Processes the received command and sends appropriate response
    /// </summary>
    /// <param name="message">The received protocol message</param>
    private void ProcessCommand(ProtocolMessage message)
    {
        try
        {
            switch (message.Command)
            {
                case ProtocolConstants.CMD_CREATE_CLASS: 
                    HandleCreateClass(message);
                    break;

                default:
                    // Echo por defecto
                    var echoResponse = new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        message.Command,
                        $"Echo: {message.Data}"
                    );
                    _protocolHandler.SendMessage(_clientSocket, echoResponse);
                    Console.WriteLine($"Respuesta enviada: {echoResponse}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error procesando comando: {ex.Message}");
            
            try
            {
                var errorMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Internal server error"
                );
                _protocolHandler.SendMessage(_clientSocket, errorMessage);
            }
            catch
            {
                Console.WriteLine("Error al enviar respuesta de error");
            }
        }
    }
    
    /// <summary>
    /// Processes the ClassSession creation (CR2)
    /// </summary>
    private void HandleCreateClass(ProtocolMessage message)
    {
        // Data esperada: nombre|descripcion|cupos|max|fecha|duracion|imagenBase64
        var parts = message.Data.Split('|');
        string nombre = parts[0];
        string descripcion = parts[1];
        int cupos = int.Parse(parts[2]);
        DateTime fecha = DateTime.Parse(parts[3]);
        int duracion = int.Parse(parts[4]);
        string? imagenBase64 = parts.Length > 5 ? parts[5] : null;

        string? imagenPath = null;
        if (!string.IsNullOrEmpty(imagenBase64))
        {
            Directory.CreateDirectory("Images");
            imagenPath = Path.Combine("Images", $"{Guid.NewGuid()}.png");
            File.WriteAllBytes(imagenPath, Convert.FromBase64String(imagenBase64));
        }

        var clase = _classManager.CreateClass(nombre, descripcion, cupos, fecha, duracion, imagenPath);

        var response = new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            ProtocolConstants.CMD_CREATE_CLASS,
            $"OK|{clase.Id}|{clase.Link}"
        );

        _protocolHandler.SendMessage(_clientSocket, response);
        Console.WriteLine($"Clase creada: {clase.Id} ({clase.Nombre})");
    }


    /// <summary>
    /// Handles socket exceptions
    /// </summary>
    /// <param name="ex">The socket exception</param>
    private void HandleSocketException(SocketException ex)
    {
        if (_state.ShouldShowDisconnectMessages())
        {
            Console.WriteLine($"Cliente desconectado abruptamente: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles object disposed exceptions
    /// </summary>
    private void HandleObjectDisposedException()
    {
        if (_state.ShouldShowDisconnectMessages())
        {
            Console.WriteLine("Socket del cliente fue liberado");
        }
    }

    /// <summary>
    /// Handles generic exceptions
    /// </summary>
    /// <param name="ex">The exception</param>
    private void HandleGenericException(Exception ex)
    {
        Console.WriteLine($"Error manejando cliente: {ex.Message}");
    }

    /// <summary>
    /// Cleans up the client connection
    /// </summary>
    private void CleanupClient()
    {
        _clientManager.RemoveClient(this);
        
        if (_state.IsConnected)
        {
            DisconnectClient();
        }
    }

    /// <summary>
    /// Disconnects the client cleanly
    /// </summary>
    public void DisconnectClient()
    {
        if (!_state.IsConnected)
        {
            return; // Already disconnected
        }
        
        _state.MarkAsDisconnectedByServer();
        CloseSocket();
    }

    /// <summary>
    /// Closes the client socket
    /// </summary>
    private void CloseSocket()
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
    }
}