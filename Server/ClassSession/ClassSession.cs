namespace Server.ClassSession;

public class ClassSession
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Descripcion { get; set; }
    public int CuposMaximos { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public int DuracionMinutos { get; set; }
    public string? ImagenPath { get; set; }
    public string Link { get; set; }
    public List<string> Inscriptos { get; set; } = new List<string>();
}