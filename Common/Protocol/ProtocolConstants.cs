namespace Common.Protocol;

/// <summary>
/// Constants for the communication protocol between client and server
/// </summary>
public static class ProtocolConstants
{
    /// <summary>
    /// Default server IP address
    /// </summary>
    public const string DEFAULT_SERVER_IP = "127.0.0.1";
    
    /// <summary>
    /// Default server port
    /// </summary>
    public const int DEFAULT_SERVER_PORT = 20000;
    
    /// <summary>
    /// Maximum buffer size for socket communication
    /// </summary>
    public const int MAX_BUFFER_SIZE = 1024;
    
    /// <summary>
    /// Maximum number of queued connections for the server
    /// </summary>
    public const int MAX_BACKLOG_CONNECTIONS = 10;
    
    /// <summary>
    /// Command to exit the client application
    /// </summary>
    public const string EXIT_COMMAND = "exit";
}
