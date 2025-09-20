namespace Server.Services;

using Server.ClassSession;
using System.Collections.Concurrent;
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using Common.Protocol;
using Server.Services; 

public class ClassService
{
    private static readonly Lazy<ClassService> _instance = new(() => new ClassService());
    public static ClassService Instance => _instance.Value;

    private readonly ConcurrentBag<ClassSession> _classes = new();
    private int _nextId = 1;
    private readonly object _lockNextId = new object();

    private ClassService() { }

    public ClassSession CreateClass(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imagePath)
    {
        int id;
        lock (_lockNextId)
        {
            id = _nextId++;
        }
        var newClass = new ClassSession
        {
            Id = id,
            Name = name,
            Description = description,
            MaxSeats = maxSeats,
            StartDateTime = startDateTime,
            DurationMinutes = durationMinutes,
            ImagePath = imagePath,
            Link = $"class-{Guid.NewGuid()}"
        };

        _classes.Add(newClass);
        return newClass;
    }
    
    public ClassSession CreateClassWithDetails(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imageBase64)
    {
        string? imagePath = null;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            byte[] imageBytes = Convert.FromBase64String(imageBase64);
            if (imageBytes.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("Imagen demasiado grande (máximo 5MB)");
            }
            Directory.CreateDirectory("Images");
            imagePath = Path.Combine("Images", $"{Guid.NewGuid()}.png");
            File.WriteAllBytes(imagePath, imageBytes);
        }
        
        var createdClass = CreateClass(name, description, maxSeats, startDateTime, durationMinutes, imagePath);
        
        Console.WriteLine($"Clase creada: {createdClass.Id} ({createdClass.Name})");
        return createdClass;
    }

    public IEnumerable<ClassSession> GetAllClasses() => _classes.ToList();

    public ProtocolMessage HandleCreateClass(string data, Guid clientId)
    {
        // Verificar autenticación
        if (!UserService.Instance.IsUserLoggedIn(clientId))
        {
            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ERROR,
                "Acción no permitida. Debes iniciar sesión primero."
            );
        }

        try
        {
            var parts = data.Split('|');
            if (parts.Length < 5)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Datos insuficientes para crear la clase"
                );
            }

            var name = parts[0];
            var description = parts[1];

            if (!int.TryParse(parts[2], out var maxSeats) || maxSeats <= 0)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Número de cupos inválido"
                );
            }
            if (!DateTime.TryParse(parts[3], out var startDateTime))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Fecha inválida"
                );
            }
            if (!int.TryParse(parts[4], out var durationMinutes) || durationMinutes <= 0)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Duración inválida"
                );
            }

            var imageBase64 = parts.Length > 5 ? parts[5] : null;

            var createdClass = CreateClassWithDetails(
                name, description, maxSeats, startDateTime, durationMinutes, imageBase64
            );

            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_CREATE_CLASS,
                $"OK|{createdClass.Id}|{createdClass.Link}"
            );
        }
        catch (ArgumentException ex)
        {
            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ERROR,
                ex.Message
            );
        }
        catch (Exception ex)
        {
            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ERROR,
                $"Error inesperado: {ex.Message}"
            );
        }
    }

    public ProtocolMessage HandleListClasses()
    {
        var classes = GetAllClasses();
        var sb = new StringBuilder();
        
        foreach (var c in classes)
        {
            string hasImage = string.IsNullOrEmpty(c.ImagePath) ? "No" : "Sí";
            int enrolled = c.EnrolledUsers.Count;

            sb.AppendLine($"{c.Id} | {c.Name} | {c.Description} | " +
                          $"{c.StartDateTime:yyyy-MM-dd HH:mm} | {c.DurationMinutes} min | " +
                          $"Cupos: {enrolled}/{c.MaxSeats} | Imagen: {hasImage}");
        }

        return new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            ProtocolConstants.CMD_LIST_CLASSES,
            sb.ToString()
        );
    }
}