using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("================================");
Console.WriteLine("   Cliente de Chat - Consola");
Console.WriteLine("================================");
Console.WriteLine("Escribí ':quit' en cualquier momento para salir.\n");

var defaultUrl = Environment.GetEnvironmentVariable("CHAT_SERVER_URL") ?? "ws://localhost:8080/chat";
var serverUrl = Prompt($"URL del servidor de chat [{defaultUrl}]", allowEmpty: true);
if (string.IsNullOrWhiteSpace(serverUrl))
{
    serverUrl = defaultUrl;
}

var username = Prompt("Usuario");
var password = Prompt("Contraseña");
var classLink = Prompt("Link de la clase");

var targetUri = BuildWebSocketUri(serverUrl, username, password, classLink);

using var socket = new ClientWebSocket();
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    Console.WriteLine($"\nConectando a {targetUri} ...");
    await socket.ConnectAsync(targetUri, cts.Token);
    Console.WriteLine("Conexión establecida. Podés comenzar a escribir mensajes.");

    var receiveTask = ReceiveLoopAsync(socket, cts.Token);
    var sendTask = SendLoopAsync(socket, cts);

    await Task.WhenAny(receiveTask, sendTask);
    cts.Cancel();
    await Task.WhenAll(receiveTask, sendTask);
}
catch (OperationCanceledException)
{
    // Cancelado por el usuario
}
catch (WebSocketException ex)
{
    Console.WriteLine($"Error al conectar con el servidor: {ex.Message}");
}
finally
{
    if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
    {
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
    }
}

static string Prompt(string label, bool allowEmpty = false)
{
    while (true)
    {
        Console.Write($"{label}: ");
        var value = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        if (allowEmpty)
        {
            return string.Empty;
        }

        Console.WriteLine("El valor es obligatorio.");
    }
}

static Uri BuildWebSocketUri(string baseUrl, string username, string password, string link)
{
    var sanitized = baseUrl.Trim();
    if (!sanitized.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) &&
        !sanitized.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
    {
        sanitized = $"ws://{sanitized.TrimStart('/')}";
    }

    sanitized = sanitized.TrimEnd('/');

    var query = $"username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}&link={Uri.EscapeDataString(link)}";

    var separator = sanitized.Contains('?') ? "&" : "?";
    return new Uri($"{sanitized}{separator}{query}");
}

static async Task SendLoopAsync(ClientWebSocket socket, CancellationTokenSource cts)
{
    while (!cts.IsCancellationRequested && socket.State == WebSocketState.Open)
    {
        var message = Console.ReadLine();
        if (message is null)
        {
            continue;
        }

        if (message.Equals(":quit", StringComparison.OrdinalIgnoreCase))
        {
            cts.Cancel();
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Cliente se desconectó", CancellationToken.None);
            }
            break;
        }

        var buffer = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}

static async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken cancellationToken)
{
    var buffer = new byte[4096];

    while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
    {
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor cerró la conexión", CancellationToken.None);
                Console.WriteLine("El servidor cerró la conexión.");
                return;
            }

            stream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        var message = Encoding.UTF8.GetString(stream.ToArray());
        PrintServerMessage(message);
    }
}

static void PrintServerMessage(string payload)
{
    try
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : "message";
        var timestampString = root.TryGetProperty("timestamp", out var tsProp) ? tsProp.GetString() : null;
        var timestamp = TryFormatTimestamp(timestampString);

        switch (type)
        {
            case "system":
            case "welcome":
                var systemMessage = root.TryGetProperty("mensaje", out var msgProp) ? msgProp.GetString() : payload;
                Console.WriteLine($"[{timestamp}] * {systemMessage}");
                break;
            case "message":
                var user = root.TryGetProperty("usuario", out var userProp) ? userProp.GetString() : "???";
                var text = root.TryGetProperty("mensaje", out var textProp) ? textProp.GetString() : payload;
                Console.WriteLine($"[{timestamp}] {user}: {text}");
                break;
            default:
                Console.WriteLine($"[{timestamp}] {payload}");
                break;
        }
    }
    catch (JsonException)
    {
        Console.WriteLine($"[Servidor] {payload}");
    }
}

static string TryFormatTimestamp(string? timestamp)
{
    if (!string.IsNullOrWhiteSpace(timestamp) &&
        DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var date))
    {
        return date.ToLocalTime().ToString("HH:mm:ss");
    }

    return DateTime.Now.ToString("HH:mm:ss");
}
