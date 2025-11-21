
using System.Text;
using System.Text.Json;

namespace Server.Services;

public class WebhookNotificationService
{
    private readonly ClassService _classService;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10); 
    
    public WebhookNotificationService(ClassService classService)
    {
        _classService = classService;
        _httpClient = new HttpClient();
    }

    public async Task StartAsync(CancellationToken token)
    {
        Console.WriteLine("Iniciando servicio de notificaciones Webhook...");

        while (!token.IsCancellationRequested)
        {
            try
            {
                await CheckAndNotifyClasses();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en servicio de notificaciones: {ex.Message}");
            }

            await Task.Delay(_checkInterval, token);
        }
    }

    private async Task CheckAndNotifyClasses()
    {
        var allClasses = _classService.GetAllClasses();
        var now = DateTime.Now;

        foreach (var classSession in allClasses)
        {
            var timeUntilStart = classSession.StartDateTime - now;
            
            if (timeUntilStart.TotalSeconds <= 60 && timeUntilStart.TotalSeconds > -30)
            {
                foreach (var enrollment in classSession.Enrollments)
                {
                    if (!string.IsNullOrEmpty(enrollment.WebhookUrl) && 
                        !enrollment.IsCancelled && 
                        !enrollment.NotificationSent)
                    {
                        await SendNotificationAsync(enrollment, classSession.Name);
                    }
                }
            }
        }
    }

    private async Task SendNotificationAsync(Server.Data.ClassSession.Enrollment enrollment, string className)
    {
        try
        {
            var payload = new
            {
                message = $"¡Tu clase '{className}' está por comenzar en 1 minuto!",
                student = enrollment.Username,
                timestamp = DateTime.Now
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"Enviando webhook a {enrollment.Username} ({enrollment.WebhookUrl})...");
            
            var response = await _httpClient.PostAsync(enrollment.WebhookUrl, content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Webhook enviado correctamente a {enrollment.Username}");
            }
            else
            {
                Console.WriteLine($"Fallo al enviar webhook: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Excepción enviando webhook: {ex.Message}");
        }
        finally
        {
            enrollment.NotificationSent = true;
        }
    }
}
