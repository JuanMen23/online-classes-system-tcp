namespace Server.Services;

using Server.ClassSession;
public class ClassManager
{
    private static readonly Lazy<ClassManager> _instance = new(() => new ClassManager());
    public static ClassManager Instance => _instance.Value;
    private readonly List<ClassSession> _clases = new();
    private int _nextId = 1;

    private ClassManager() { }
    public ClassSession CreateClass(string nombre, string descripcion, int cupos,
        DateTime fechaHora, int duracion, string? imagenPath)
    {
        var nuevaClase = new ClassSession
        {
            Id = _nextId++,
            Nombre = nombre,
            Descripcion = descripcion,
            CuposMaximos = cupos,
            FechaHoraInicio = fechaHora,
            DuracionMinutos = duracion,
            ImagenPath = imagenPath,
            Link = $"class-{Guid.NewGuid()}"
        };

        _clases.Add(nuevaClase);
        return nuevaClase;
    }

    public IEnumerable<ClassSession> GetAllClasses() => _clases;
}