using Common.Proto;
using Grpc.Core;
using Server.Services;

namespace Server.GrpcServices;

public class ClassVerificationGrpcService : ClassVerificationService.ClassVerificationServiceBase
{
    private readonly ClassService _classService;

    public ClassVerificationGrpcService(ClassService classService)
    {
        _classService = classService;
    }

    public override Task<ClassLinkResponse> VerifyClassLink(ClassLinkRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Link))
        {
            return Task.FromResult(new ClassLinkResponse
            {
                Valid = false,
                Message = "El link de la clase es requerido"
            });
        }

        var classSession = _classService.GetClassByLink(request.Link);
        if (classSession is null)
        {
            return Task.FromResult(new ClassLinkResponse
            {
                Valid = false,
                Message = "Clase no encontrada"
            });
        }

        return Task.FromResult(new ClassLinkResponse
        {
            Valid = true,
            ClassId = classSession.Id,
            ClassName = classSession.Name,
            Message = "Clase verificada"
        });
    }

    public override Task<EnrollmentResponse> VerifyEnrollment(EnrollmentRequest request, ServerCallContext context)
    {
        if (request.ClassId <= 0 || string.IsNullOrWhiteSpace(request.Username))
        {
            return Task.FromResult(new EnrollmentResponse
            {
                Enrolled = false,
                Message = "Clase y usuario son requeridos"
            });
        }

        bool enrolled = _classService.IsUserEnrolledInClass(request.ClassId, request.Username);
        return Task.FromResult(new EnrollmentResponse
        {
            Enrolled = enrolled,
            Message = enrolled ? "Usuario inscrito en la clase" : "Usuario no inscrito en la clase"
        });
    }
}

