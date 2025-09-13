namespace Server.Services;

/// <summary>
/// Manages the state of a client connection
/// </summary>
public class ClientState
{
    /// <summary>
    /// Gets or sets whether the client is connected
    /// </summary>
    public bool IsConnected { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the client was disconnected by server shutdown
    /// </summary>
    public bool WasDisconnectedByServer { get; set; } = false;

    /// <summary>
    /// Marks the client as disconnected by server shutdown
    /// </summary>
    public void MarkAsDisconnectedByServer()
    {
        WasDisconnectedByServer = true;
        IsConnected = false;
    }

    /// <summary>
    /// Marks the client as disconnected naturally
    /// </summary>
    public void MarkAsDisconnectedNaturally()
    {
        IsConnected = false;
    }

    /// <summary>
    /// Checks if the client should show disconnect messages
    /// </summary>
    /// <returns>True if disconnect messages should be shown</returns>
    public bool ShouldShowDisconnectMessages()
    {
        return !WasDisconnectedByServer;
    }
}
