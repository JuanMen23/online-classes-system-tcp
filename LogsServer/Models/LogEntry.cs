namespace LogsServer.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Usuario { get; set; } = string.Empty;
    public int? ClaseId { get; set; }
    public string Evento { get; set; } = string.Empty;
    public string Nivel { get; set; } = "INFO";
    public string Mensaje { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

