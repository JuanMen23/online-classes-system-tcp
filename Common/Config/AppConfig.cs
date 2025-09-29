using System.Net;
using Common.Protocol;

namespace Common.Config;

/// <summary>
/// Configuration class for application settings.
/// Reads from App.config with safe fallbacks.
/// </summary>
public class AppConfig
{
    private readonly SettingsManager _settings = new();
    
    /// <summary>
    /// Server IP address
    /// </summary>
    public string ServerIp { get; set; }
    
    /// <summary>
    /// Server port
    /// </summary>
    public int ServerPort { get; set; }
    
    /// <summary>
    /// Server IP address
    /// </summary>
    public string ClientIp { get; set; }
    
    /// <summary>
    /// Server port
    /// </summary>
    public int ClientPort { get; set; }
    
    /// <summary>
    /// Maximum number of queued connections
    /// </summary>
    public int MaxBacklogConnections { get; set; } = ProtocolConstants.MAX_BACKLOG_CONNECTIONS;

    public AppConfig()
    {
        ServerIp = _settings.ReadSettings(IpConfig.serverIPconfigKey) 
                   ?? ProtocolConstants.DEFAULT_SERVER_IP;
        string? serverPortStr = _settings.ReadSettings(IpConfig.serverPortconfigKey);
        ServerPort = int.TryParse(serverPortStr, out int parsedServerPort) 
            ? parsedServerPort 
            : ProtocolConstants.DEFAULT_SERVER_PORT;
        
        ClientIp = _settings.ReadSettings(IpConfig.clientIPconfigKey) 
                   ?? "127.0.0.1";
        
        string? clientPortStr = _settings.ReadSettings(IpConfig.clientPortconfigKey);
        ClientPort = int.TryParse(clientPortStr, out int parsedClientPort) 
            ? parsedClientPort 
            : 0;
    }
    
    /// <summary>
    /// Gets the server endpoint
    /// </summary>
    public IPEndPoint GetServerEndPoint()
    {
        Console.WriteLine($"SERVER IP {ServerIp} + SERVER PORT: {ServerPort}");
        return new IPEndPoint(IPAddress.Parse(ServerIp), ServerPort);
        
    }
    
    /// <summary>
    /// Gets the local endpoint for client binding
    /// </summary>
    public IPEndPoint GetLocalEndPoint()
    {
        Console.WriteLine($"CLIENT IP {ClientIp} + CLIENT PORT {ClientPort} + SERVER IP {ServerIp} + SERVER PORT: {ServerPort}");
        return new IPEndPoint(IPAddress.Parse(ClientIp), ClientPort);
    }
}
