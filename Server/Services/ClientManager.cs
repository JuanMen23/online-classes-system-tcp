using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Server.Services;

/// <summary>
/// Singleton class that manages all connected clients
/// </summary>
public class ClientManager
{
    private static readonly Lazy<ClientManager> _instance = new(() => new ClientManager());
    private readonly ConcurrentDictionary<Guid, ClientHandler> _connectedClients;
    private readonly object _lock = new object();

    /// <summary>
    /// Gets the singleton instance of ClientManager
    /// </summary>
    public static ClientManager Instance => _instance.Value;

    /// <summary>
    /// Private constructor for singleton pattern
    /// </summary>
    private ClientManager()
    {
        _connectedClients = new ConcurrentDictionary<Guid, ClientHandler>();
    }

    /// <summary>
    /// Adds a client to the managed list
    /// </summary>
    /// <param name="client">The client handler to add</param>
    public void AddClient(ClientHandler client)
    {
        if (client != null)
        {
            _connectedClients.TryAdd(client.Id, client);
            var total = GetConnectedClientsCount();
            Console.WriteLine($"Cliente conectado. Total: {total}");
            LoggingService.Instance.PublishLog(
                "client_connected",
                "system",
                $"Cliente conectado ({client.Id})",
                metadata: new Dictionary<string, string>
                {
                    ["client_id"] = client.Id.ToString(),
                    ["total_connected"] = total.ToString()
                });
        }
    }

    /// <summary>
    /// Removes a client from the managed list
    /// </summary>
    /// <param name="client">The client handler to remove</param>
    public void RemoveClient(ClientHandler client)
    {
        if (client != null)
        {
            UserService.Instance.LogoutUser(client.Id, "connection_closed");
            if (_connectedClients.TryRemove(client.Id, out _))
            {
                var total = GetConnectedClientsCount();
                Console.WriteLine($"Cliente desconectado. Total: {total}");
                LoggingService.Instance.PublishLog(
                    "client_disconnected",
                    "system",
                    $"Cliente desconectado ({client.Id})",
                    metadata: new Dictionary<string, string>
                    {
                        ["client_id"] = client.Id.ToString(),
                        ["total_connected"] = total.ToString()
                    });
            }
        }
    }

    /// <summary>
    /// Gets the current number of connected clients
    /// </summary>
    /// <returns>Number of connected clients</returns>
    public int GetConnectedClientsCount()
    {
        return _connectedClients.Count;
    }

    /// <summary>
    /// Disconnects all connected clients gracefully
    /// </summary>
    public void DisconnectAllClients()
    {
        lock (_lock)
        {
            var clientsToDisconnect = _connectedClients.Values.ToList();
            int totalClients = clientsToDisconnect.Count;

            if (totalClients == 0)
            {
                Console.WriteLine("No hay clientes conectados para desconectar.");
                return;
            }

            Console.WriteLine($"Desconectando {totalClients} clientes...");
            LoggingService.Instance.PublishLog(
                "client_disconnect_all",
                "system",
                $"Desconectando {totalClients} clientes por cierre del servidor",
                metadata: new Dictionary<string, string> { ["total"] = totalClients.ToString() });

            foreach (var client in clientsToDisconnect)
            {
                try
                {
                    client.DisconnectClient();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error desconectando cliente: {ex.Message}");
                }
            }

            Console.WriteLine("Todos los clientes desconectados");
            LoggingService.Instance.PublishLog(
                "client_disconnect_all_completed",
                "system",
                "Todos los clientes fueron desconectados correctamente");
        }
    }

    /// <summary>
    /// Gets a copy of all connected clients
    /// </summary>
    /// <returns>List of connected client handlers</returns>
    public List<ClientHandler> GetAllClients()
    {
        return _connectedClients.Values.ToList();
    }

}

