using System;

namespace LogsServer.Models;

public class LogFilterOptions
{
    public string? Usuario { get; set; }
    public string? Evento { get; set; }
    public string? Nivel { get; set; }
    public int? ClaseId { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? Contiene { get; set; }
    public int Limit { get; set; } = 100;
}

