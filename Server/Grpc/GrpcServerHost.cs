using Common.Config;
using Common.Proto;
using Grpc.Core;
using Server.Services;

namespace Server.GrpcServices;

public class GrpcServerHost : IAsyncDisposable
{
    private readonly AppConfig _config;
    private Grpc.Core.Server? _server;

    public GrpcServerHost(AppConfig config)
    {
        _config = config;
    }

    public Task StartAsync()
    {
        if (_server != null)
        {
            return Task.CompletedTask;
        }

        _server = new Grpc.Core.Server
        {
            Services =
            {
                AuthenticationService.BindService(new AuthenticationGrpcService(UserService.Instance)),
                ClassVerificationService.BindService(new ClassVerificationGrpcService(ClassService.Instance))
            },
            Ports = { new ServerPort(_config.GrpcServerHost, _config.GrpcServerPort, ServerCredentials.Insecure) }
        };

        _server.Start();
        Console.WriteLine($"Servidor gRPC escuchando en {_config.GrpcServerHost}:{_config.GrpcServerPort}");

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_server != null)
        {
            await _server.ShutdownAsync();
            Console.WriteLine("Servidor gRPC detenido");
            _server = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}

