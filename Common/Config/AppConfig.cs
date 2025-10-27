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
    /// Server IP address or hostname
    /// </summary>
    public string ServerIp { get; set; }

    /// <summary>
    /// Server port
    /// </summary>
    public int ServerPort { get; set; }

    /// <summary>
    /// Client IP address
    /// </summary>
    public string ClientIp { get; set; }

    /// <summary>
    /// Client port
    /// </summary>
    public int ClientPort { get; set; }

    /// <summary>
    /// Maximum number of queued connections
    /// </summary>
    public int MaxBacklogConnections { get; set; } = ProtocolConstants.MAX_BACKLOG_CONNECTIONS;

    public AppConfig()
    {
        // Leer configuración del servidor
        ServerIp = _settings.ReadSettings(IpConfig.serverIPconfigKey)
                   ?? ProtocolConstants.DEFAULT_SERVER_IP;

        string? serverPortStr = _settings.ReadSettings(IpConfig.serverPortconfigKey);
        ServerPort = int.TryParse(serverPortStr, out int parsedServerPort)
            ? parsedServerPort
            : ProtocolConstants.DEFAULT_SERVER_PORT;

        // Leer configuración del cliente
        ClientIp = _settings.ReadSettings(IpConfig.clientIPconfigKey)
                   ?? "0.0.0.0"; // 0.0.0.0 para binding dinámico

        string? clientPortStr = _settings.ReadSettings(IpConfig.clientPortconfigKey);
        ClientPort = int.TryParse(clientPortStr, out int parsedClientPort)
            ? parsedClientPort
            : 0; // 0 = puerto dinámico
    }

    /// <summary>
    /// Obtiene el endpoint del servidor resolviendo nombres de host si es necesario
    /// </summary>
    public IPEndPoint GetServerEndPoint()
    {
        Console.WriteLine($"SERVER IP/HOST {ServerIp} + SERVER PORT: {ServerPort}");
        
        IPAddress ipAddress;

        // Intentar parsear como IP; si falla, resolver como hostname
        if (!IPAddress.TryParse(ServerIp, out ipAddress))
        {
            // Resuelve el nombre del contenedor o host dentro de la red Docker
            IPAddress[] addresses = Dns.GetHostAddresses(ServerIp);
            if (addresses.Length == 0)
                throw new Exception($"No se pudo resolver el host '{ServerIp}'");

            ipAddress = addresses[0];
        }

        return new IPEndPoint(ipAddress, ServerPort);
    }

    /// <summary>
    /// Obtiene el endpoint local del cliente
    /// </summary>
    public IPEndPoint GetLocalEndPoint()
    {
        Console.WriteLine($"CLIENT IP {ClientIp} + CLIENT PORT {ClientPort}");
        
        IPAddress ipAddress;

        if (!IPAddress.TryParse(ClientIp, out ipAddress))
        {
            IPAddress[] addresses = Dns.GetHostAddresses(ClientIp);
            if (addresses.Length == 0)
                throw new Exception($"No se pudo resolver el host '{ClientIp}'");

            ipAddress = addresses[0];
        }

        return new IPEndPoint(ipAddress, ClientPort);
    }
}
