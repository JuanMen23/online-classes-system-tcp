using System.Net;

namespace Common.Config;

/// <summary>
/// Configuration class for application settings
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Server IP address
    /// </summary>
    public string ServerIp { get; set; } = Protocol.ProtocolConstants.DEFAULT_SERVER_IP;
    
    /// <summary>
    /// Server port
    /// </summary>
    public int ServerPort { get; set; } = Protocol.ProtocolConstants.DEFAULT_SERVER_PORT;
    
    /// <summary>
    /// Maximum number of queued connections
    /// </summary>
    public int MaxBacklogConnections { get; set; } = Protocol.ProtocolConstants.MAX_BACKLOG_CONNECTIONS;
    
    /// <summary>
    /// Gets the server endpoint
    /// </summary>
    public IPEndPoint GetServerEndPoint()
    {
        return new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
    }
    
    /// <summary>
    /// Gets the local endpoint for client binding
    /// </summary>
    public IPEndPoint GetLocalEndPoint()
    {
        return new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0); // Port 0 = auto-assign
    }
}
