using LogsServer.Models;

namespace LogsServer.Services;

/// <summary>
/// Interfaz para gestionar el almacenamiento y recuperación de logs
/// </summary>
public interface ILogService
{
    /// <summary>Agrega un log al almacenamiento</summary>
    void AddLog(LogEntry log);

    /// <summary>Obtiene todos los logs</summary>
    IEnumerable<LogEntry> GetAll();

    /// <summary>Filtra logs por usuario</summary>
    IEnumerable<LogEntry> FilterByUsuario(string usuario);

    /// <summary>Filtra logs por evento</summary>
    IEnumerable<LogEntry> FilterByEvento(string evento);

    /// <summary>Filtra logs con múltiples criterios</summary>
    IEnumerable<LogEntry> Filter(LogFilterOptions options);
}

