using Common.Proto;
using Grpc.Core;
using Server.Services;

namespace Server.GrpcServices;

public class AuthenticationGrpcService : AuthenticationService.AuthenticationServiceBase
{
    private readonly UserService _userService;

    public AuthenticationGrpcService(UserService userService)
    {
        _userService = userService;
    }

    public override Task<ValidateResponse> ValidateCredentials(ValidateRequest request, ServerCallContext context)
    {
        var isValid = _userService.ValidateCredentials(request.Username, request.Password);

        var response = new ValidateResponse
        {
            Valid = isValid,
            Message = isValid ? "Credenciales válidas" : "Usuario o contraseña inválidos"
        };

        return Task.FromResult(response);
    }
}

