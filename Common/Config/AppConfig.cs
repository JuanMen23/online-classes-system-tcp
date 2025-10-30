using System.Net;
using Common.Protocol;

namespace Common.Config;

/// <summary>
/// Configuration class for application settings.
/// Reads from environment variables or defaults.
/// </summary>
public class AppConfig
{
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
        // Leer del environment
        ServerIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "127.0.0.1";
        ServerPort = int.TryParse(Environment.GetEnvironmentVariable("SERVER_PORT"), out var sp)
            ? sp
            : ProtocolConstants.DEFAULT_SERVER_PORT;

        ClientIp = Environment.GetEnvironmentVariable("CLIENT_IP") ?? "0.0.0.0";
        ClientPort = int.TryParse(Environment.GetEnvironmentVariable("CLIENT_PORT"), out var cp)
            ? cp
            : 0;

        Console.WriteLine($"[DEBUG] SERVER_IP={ServerIp}, SERVER_PORT={ServerPort}");
        Console.WriteLine($"[DEBUG] CLIENT_IP={ClientIp}, CLIENT_PORT={ClientPort}");
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
    public string ServerImageDirectory { get; set; } = "/app/Images";

}
