namespace Common.Protocol;

/// <summary>
/// Constants for the communication protocol between client and server
/// </summary>
public static class ProtocolConstants
{
    // Network Configuration
    /// <summary>
    /// Default server IP address
    /// </summary>
    public const string DEFAULT_SERVER_IP = "127.0.0.1";
    
    /// <summary>
    /// Default server port
    /// </summary>
    public const int DEFAULT_SERVER_PORT = 20000;
    
    /// <summary>
    /// Maximum number of queued connections for the server
    /// </summary>
    public const int MAX_BACKLOG_CONNECTIONS = 10;
    
    // Protocol Structure
    /// <summary>
    /// Length of the HEADER field in characters
    /// </summary>
    public const int HEADER_LENGTH = 3;
    
    /// <summary>
    /// Length of the CMD field in characters
    /// </summary>
    public const int CMD_LENGTH = 2;
    
    /// <summary>
    /// Length of the LARGO field in characters
    /// </summary>
    public const int LARGO_LENGTH = 4;
    
    // Header Values
    /// <summary>
    /// Request header value
    /// </summary>
    public const string HEADER_REQUEST = "REQ";
    
    /// <summary>
    /// Response header value
    /// </summary>
    public const string HEADER_RESPONSE = "RES";
    
    // Commands (Authentication only)
    /// <summary>
    /// Login command
    /// </summary>
    public const int CMD_LOGIN = 1;
    
    /// <summary>
    /// Logout command
    /// </summary>
    public const int CMD_LOGOUT = 2;
    
    /// <summary>
    /// Register command
    /// </summary>
    public const int CMD_REGISTER = 3;
    
    /// <summary>
    /// Error command
    /// </summary>
    public const int CMD_ERROR = 99;
    
    // Legacy
    /// <summary>
    /// Command to exit the client application
    /// </summary>
    public const string EXIT_COMMAND = "exit";
}
