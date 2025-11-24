using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ChatServer.Models;
using ChatServer.Services;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ChatRoomManager>();
builder.Services.AddSingleton<GrpcAuthClient>();
var app = builder.Build();
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
};

app.UseWebSockets(webSocketOptions);

app.MapGet("/", () => Results.Json(new { service = "ChatServer", status = "running" }));
app.MapGet("/health", () => Results.Ok("ok"));

app.Map("/chat", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Se requiere una conexión WebSocket.");
        return;
    }

    var username = context.Request.Query["username"].ToString();
    var password = context.Request.Query["password"].ToString();
    var link = context.Request.Query["link"].ToString();

    if (string.IsNullOrWhiteSpace(username) ||
        string.IsNullOrWhiteSpace(password) ||
        string.IsNullOrWhiteSpace(link))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("username, password y link son obligatorios.");
        return;
    }

    var grpcClient = context.RequestServices.GetRequiredService<GrpcAuthClient>();
    var validation = await grpcClient.ValidateConnectionAsync(username, password, link, context.RequestAborted);

    if (!validation.Success)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(validation.Message);
        return;
    }

    var roomManager = context.RequestServices.GetRequiredService<ChatRoomManager>();
    var room = roomManager.GetOrCreateRoom(link, validation.ClassId, validation.ClassName);

    if (room.ContainsUser(username))
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsync("El usuario ya está conectado a esta clase.");
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    room.TryAddParticipant(username, webSocket);

    await SendJsonAsync(webSocket, new
    {
        type = "welcome",
        mensaje = $"Conectado a la clase {validation.ClassName}",
        timestamp = DateTime.UtcNow
    }, jsonOptions, context.RequestAborted);

    await BroadcastAsync(room, roomManager, new
    {
        type = "system",
        mensaje = $"{username} se unió al chat",
        timestamp = DateTime.UtcNow
    }, jsonOptions, context.RequestAborted, excludeUser: username);

    try
    {
        await ReceiveMessagesAsync(room, username, webSocket, roomManager, jsonOptions, context.RequestAborted);
    }
    finally
    {
        room.TryRemoveParticipant(username, out _);
        roomManager.RemoveRoomIfEmpty(room.Link);

        await BroadcastAsync(room, roomManager, new
        {
            type = "system",
            mensaje = $"{username} salió del chat",
            timestamp = DateTime.UtcNow
        }, jsonOptions, context.RequestAborted, excludeUser: username);
    }
});

app.Run();

static async Task ReceiveMessagesAsync(
    ChatRoom room,
    string username,
    WebSocket socket,
    ChatRoomManager manager,
    JsonSerializerOptions jsonOptions,
    CancellationToken cancellationToken)
{
    var buffer = new byte[4 * 1024];

    while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
    {
        WebSocketReceiveResult result;
        try
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);
        }
        catch
        {
            break;
        }

        if (result.MessageType == WebSocketMessageType.Close)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cierre solicitado", cancellationToken);
            break;
        }

        var message = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
        if (string.IsNullOrEmpty(message))
        {
            continue;
        }

        await BroadcastAsync(room, manager, new
        {
            type = "message",
            usuario = username,
            mensaje = message,
            timestamp = DateTime.UtcNow
        }, jsonOptions, cancellationToken);
    }
}

static async Task BroadcastAsync(
    ChatRoom room,
    ChatRoomManager manager,
    object payload,
    JsonSerializerOptions jsonOptions,
    CancellationToken cancellationToken,
    string? excludeUser = null)
{
    var json = JsonSerializer.Serialize(payload, jsonOptions);
    var bytes = Encoding.UTF8.GetBytes(json);
    var segment = new ArraySegment<byte>(bytes);

    foreach (var (participant, socket) in room.Participants)
    {
        if (!string.IsNullOrEmpty(excludeUser) &&
            participant.Equals(excludeUser, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (socket.State != WebSocketState.Open)
        {
            continue;
        }

        try
        {
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
        }
        catch
        {
            room.TryRemoveParticipant(participant, out _);
            manager.RemoveRoomIfEmpty(room.Link);
        }
    }
}

static Task SendJsonAsync(WebSocket socket, object payload, JsonSerializerOptions options, CancellationToken cancellationToken)
{
    var json = JsonSerializer.Serialize(payload, options);
    var bytes = Encoding.UTF8.GetBytes(json);
    return socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
}
