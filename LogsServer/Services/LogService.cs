using System;
using System.Collections.Concurrent;
using System.Linq;
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

    public IEnumerable<LogEntry> Filter(LogFilterOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var query = GetAll();

        if (!string.IsNullOrWhiteSpace(options.Usuario))
            query = query.Where(l => l.Usuario.Contains(options.Usuario, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(options.Evento))
            query = query.Where(l => l.Evento.Contains(options.Evento, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(options.Nivel))
            query = query.Where(l => l.Nivel.Equals(options.Nivel, StringComparison.OrdinalIgnoreCase));

        if (options.ClaseId.HasValue)
            query = query.Where(l => l.ClaseId == options.ClaseId);

        if (options.Desde.HasValue)
            query = query.Where(l => l.Timestamp >= options.Desde.Value);

        if (options.Hasta.HasValue)
            query = query.Where(l => l.Timestamp <= options.Hasta.Value);

        if (!string.IsNullOrWhiteSpace(options.Contiene))
        {
            query = query.Where(l =>
                l.Mensaje.Contains(options.Contiene, StringComparison.OrdinalIgnoreCase) ||
                l.Metadata.Any(kvp =>
                    kvp.Key.Contains(options.Contiene, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Value.Contains(options.Contiene, StringComparison.OrdinalIgnoreCase)));
        }

        var limit = Math.Clamp(options.Limit, 1, 500);
        return query.Take(limit);
    }
}

