using Microsoft.AspNetCore.Mvc;
using LogsServer.Models;
using LogsServer.Services;

namespace LogsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogService logService, ILogger<LogsController> logger)
    {
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Obtiene todos los logs con filtros opcionales</summary>
    [HttpGet]
    public IActionResult GetLogs([FromQuery] string? usuario, [FromQuery] string? evento, [FromQuery] int limit = 100)
    {
        try
        {
            var logs = _logService.Filter(usuario, evento, limit).ToList();
            _logger.LogInformation($"📊 Se consultaron {logs.Count} logs");

            return Ok(new
            {
                logs,
                total = logs.Count,
                filters = new { usuario, evento, limit }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al obtener logs");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Agrega un log de prueba (para testing)</summary>
    [HttpPost("test")]
    public IActionResult AddTestLog([FromBody] LogEntry log)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(log.Evento) || string.IsNullOrWhiteSpace(log.Mensaje))
                return BadRequest("evento y mensaje son requeridos");

            _logService.AddLog(log);
            _logger.LogInformation($"✅ Log de prueba agregado: {log.Evento}");

            return Ok(new { message = "Log agregado", log });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al agregar log");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

