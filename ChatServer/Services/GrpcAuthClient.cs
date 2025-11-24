using Common.Proto;
using Grpc.Net.Client;

namespace ChatServer.Services;

public class GrpcAuthClient : IAsyncDisposable
{
    private readonly GrpcChannel _channel;
    private readonly AuthenticationService.AuthenticationServiceClient _authClient;
    private readonly ClassVerificationService.ClassVerificationServiceClient _classClient;

    public GrpcAuthClient(IConfiguration configuration)
    {
        var serverUrl = Environment.GetEnvironmentVariable("GRPC_SERVER_URL")
                        ?? configuration["Grpc:ServerUrl"]
                        ?? "http://localhost:50051";

        _channel = GrpcChannel.ForAddress(serverUrl);
        _authClient = new AuthenticationService.AuthenticationServiceClient(_channel);
        _classClient = new ClassVerificationService.ClassVerificationServiceClient(_channel);
    }

    public async Task<ConnectionValidationResult> ValidateConnectionAsync(
        string username,
        string password,
        string link,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(link))
        {
            return ConnectionValidationResult.Error("username, password y link son obligatorios.");
        }

        var credentialResponse = await _authClient.ValidateCredentialsAsync(
            new ValidateRequest { Username = username, Password = password },
            cancellationToken: cancellationToken);

        if (!credentialResponse.Valid)
        {
            return ConnectionValidationResult.Error(credentialResponse.Message);
        }

        var classResponse = await _classClient.VerifyClassLinkAsync(
            new ClassLinkRequest { Link = link },
            cancellationToken: cancellationToken);

        if (!classResponse.Valid)
        {
            return ConnectionValidationResult.Error(classResponse.Message);
        }

        return ConnectionValidationResult.Ok(
            classResponse.ClassId,
            classResponse.ClassName,
            "Validación exitosa");
    }

    public ValueTask DisposeAsync()
    {
        _channel.Dispose();
        return ValueTask.CompletedTask;
    }
}

public record ConnectionValidationResult(bool Success, int ClassId, string ClassName, string Message)
{
    public static ConnectionValidationResult Error(string message) => new(false, 0, string.Empty, message);
    public static ConnectionValidationResult Ok(int classId, string className, string message) =>
        new(true, classId, className, message);
}

