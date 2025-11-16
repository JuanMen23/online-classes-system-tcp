using System.Collections.Concurrent;
using LogsServer.Models;

namespace LogsServer.Services;

/// <summary>
/// Servicio thread-safe para almacenar y recuperar logs en memoria
/// </summary>
public class LogService : ILogService
{
    private readonly ConcurrentBag<LogEntry> _logs = new();

    public void AddLog(LogEntry log)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        _logs.Add(log);
    }

    public IEnumerable<LogEntry> GetAll()
    {
        return _logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    public IEnumerable<LogEntry> FilterByUsuario(string usuario)
    {
        return GetAll().Where(l => l.Usuario.Equals(usuario, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<LogEntry> FilterByEvento(string evento)
    {
        return GetAll().Where(l => l.Evento.Equals(evento, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<LogEntry> Filter(string? usuario = null, string? evento = null, int limit = 100)
    {
        var query = GetAll();

        if (!string.IsNullOrWhiteSpace(usuario))
            query = query.Where(l => l.Usuario.Contains(usuario, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(evento))
            query = query.Where(l => l.Evento.Contains(evento, StringComparison.OrdinalIgnoreCase));

        return query.Take(limit);
    }
}

