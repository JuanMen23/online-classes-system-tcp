using System.Net;
using Common.Protocol;

namespace Common.Config;

/// <summary>
/// Configuration class for application settings
/// </summary>
public class AppConfig
{
    private readonly SettingsManager _settings = new SettingsManager();
    
    /// <summary>
    /// Server IP address
    /// </summary>
    public string ServerIp { get; set; }
    
    /// <summary>
    /// Server port
    /// </summary>
    public int ServerPort { get; set; }
    
    /// <summary>
    /// Maximum number of queued connections
    /// </summary>
    public int MaxBacklogConnections { get; set; } = ProtocolConstants.MAX_BACKLOG_CONNECTIONS;

    public AppConfig()
    {
        ServerIp = _settings.ReadSettings("ServerIpAddress") ?? ProtocolConstants.DEFAULT_SERVER_IP;
        var portStr = _settings.ReadSettings("ServerPort");
        ServerPort = int.TryParse(portStr, out var parsedPort) 
            ? parsedPort 
            : ProtocolConstants.DEFAULT_SERVER_PORT;
    }
    
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
