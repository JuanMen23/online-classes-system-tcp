namespace Server.Services;

using Server.Data;
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
        DateTime startDateTime, int durationMinutes, string? imagePath, string createdBy)
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
            Link = $"class-{Guid.NewGuid()}",
            CreatedBy = createdBy
        };

        _classes.Add(newClass);
        return newClass;
    }
    
    public ClassSession CreateClassWithDetails(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imageBase64, string createdBy)
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
        
        var createdClass = CreateClass(name, description, maxSeats, startDateTime, durationMinutes, imagePath, createdBy);
        
        Console.WriteLine($"Clase creada: {createdClass.Id} ({createdClass.Name}) por {createdBy}");
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

            // Obtener el nombre de usuario actual
            var username = UserService.Instance.GetLoggedInUsername(clientId);
            if (string.IsNullOrEmpty(username))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se pudo obtener el usuario actual"
                );
            }

            var createdClass = CreateClassWithDetails(
                name, description, maxSeats, startDateTime, durationMinutes, imageBase64, username
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

    public ProtocolMessage HandleEnrollClass(string data, Guid clientId)
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
            // Parsear el ID de la clase
            if (!int.TryParse(data, out var classId))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "ID de clase inválido"
                );
            }

            // Buscar la clase
            var targetClass = _classes.FirstOrDefault(c => c.Id == classId);
            if (targetClass == null)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Clase no encontrada"
                );
            }

            // Obtener el nombre de usuario actual
            var username = UserService.Instance.GetLoggedInUsername(clientId);
            if (string.IsNullOrEmpty(username))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se pudo obtener el usuario actual"
                );
            }

            // Verificar si ya está inscrito
            if (targetClass.EnrolledUsers.Contains(username))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Ya estás inscrito en esta clase"
                );
            }

            // Verificar si hay cupos disponibles
            if (targetClass.EnrolledUsers.Count >= targetClass.MaxSeats)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No hay cupos disponibles para esta clase"
                );
            }

            // Inscribir al usuario
            targetClass.EnrolledUsers.Add(username);

            Console.WriteLine($"Usuario '{username}' inscrito en clase '{targetClass.Name}' (ID: {targetClass.Id})");

            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ENROLL_CLASS,
                $"OK|Inscripción exitosa en la clase '{targetClass.Name}'"
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

    public ProtocolMessage HandleCancelEnrollment(string data, Guid clientId)
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
            // Parsear el ID de la clase
            if (!int.TryParse(data, out var classId))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "ID de clase inválido"
                );
            }

            // Buscar la clase
            var targetClass = _classes.FirstOrDefault(c => c.Id == classId);
            if (targetClass == null)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Clase no encontrada"
                );
            }

            // Obtener el nombre de usuario actual
            var username = UserService.Instance.GetLoggedInUsername(clientId);
            if (string.IsNullOrEmpty(username))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se pudo obtener el usuario actual"
                );
            }

            // Verificar si está inscrito en la clase
            if (!targetClass.EnrolledUsers.Contains(username))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No estás inscrito en esta clase"
                );
            }

            // Validar que la cancelación se realice con al menos 2 minutos de antelación
            var timeUntilClass = targetClass.StartDateTime - DateTime.Now;
            if (timeUntilClass.TotalMinutes < 2)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se puede cancelar la inscripción. Debe hacerlo con al menos 2 minutos de antelación al inicio de la clase."
                );
            }

            // Cancelar la inscripción (remover usuario de la lista)
            targetClass.EnrolledUsers.Remove(username);

            Console.WriteLine($"Usuario '{username}' canceló su inscripción en la clase '{targetClass.Name}' (ID: {targetClass.Id})");

            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_CANCEL_ENROLL,
                $"OK|Inscripción cancelada exitosamente en la clase '{targetClass.Name}'. El cupo queda disponible para otros usuarios."
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

    public ProtocolMessage HandleModifyClass(string data, Guid clientId)
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
            if (parts.Length < 6)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Datos insuficientes para modificar la clase"
                );
            }

            // Parsear el ID de la clase
            if (!int.TryParse(parts[0], out var classId))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "ID de clase inválido"
                );
            }

            // Buscar la clase
            var targetClass = _classes.FirstOrDefault(c => c.Id == classId);
            if (targetClass == null)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Clase no encontrada"
                );
            }

            // Obtener el nombre de usuario actual
            var username = UserService.Instance.GetLoggedInUsername(clientId);
            if (string.IsNullOrEmpty(username))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se pudo obtener el usuario actual"
                );
            }

            // Verificar que el usuario sea el creador de la clase
            if (targetClass.CreatedBy != username)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Solo el creador de la clase puede modificarla"
                );
            }

            // Verificar que la clase no haya comenzado
            if (targetClass.StartDateTime <= DateTime.Now)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se puede modificar una clase que ya ha comenzado"
                );
            }

            // Parsear los nuevos datos
            var newName = parts[1];
            var newDescription = parts[2];

            if (!int.TryParse(parts[3], out var newMaxSeats) || newMaxSeats <= 0)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Número de cupos inválido"
                );
            }

            if (!DateTime.TryParse(parts[4], out var newStartDateTime))
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Fecha inválida"
                );
            }

            if (!int.TryParse(parts[5], out var newDurationMinutes) || newDurationMinutes <= 0)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Duración inválida"
                );
            }

            // Verificar que el nuevo número de cupos no sea menor al número de usuarios inscritos
            if (newMaxSeats < targetClass.EnrolledUsers.Count)
            {
                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    $"No se puede reducir los cupos por debajo del número de usuarios inscritos ({targetClass.EnrolledUsers.Count})"
                );
            }

            // Procesar imagen si se proporciona
            var newImageBase64 = parts.Length > 6 ? parts[6] : null;
            string? newImagePath = targetClass.ImagePath; // Mantener la imagen actual por defecto

            if (!string.IsNullOrEmpty(newImageBase64))
            {
                try
                {
                    byte[] imageBytes = Convert.FromBase64String(newImageBase64);
                    if (imageBytes.Length > 5 * 1024 * 1024)
                    {
                        return new ProtocolMessage(
                            ProtocolConstants.HEADER_RESPONSE,
                            ProtocolConstants.CMD_ERROR,
                            "Imagen demasiado grande (máximo 5MB)"
                        );
                    }

                    // Eliminar imagen anterior si existe
                    if (!string.IsNullOrEmpty(targetClass.ImagePath) && File.Exists(targetClass.ImagePath))
                    {
                        File.Delete(targetClass.ImagePath);
                    }

                    // Guardar nueva imagen
                    Directory.CreateDirectory("Images");
                    newImagePath = Path.Combine("Images", $"{Guid.NewGuid()}.png");
                    File.WriteAllBytes(newImagePath, imageBytes);
                }
                catch (FormatException)
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        "Formato de imagen inválido"
                    );
                }
            }

            // Actualizar los datos de la clase
            targetClass.Name = newName;
            targetClass.Description = newDescription;
            targetClass.MaxSeats = newMaxSeats;
            targetClass.StartDateTime = newStartDateTime;
            targetClass.DurationMinutes = newDurationMinutes;
            targetClass.ImagePath = newImagePath;

            Console.WriteLine($"Clase modificada: {targetClass.Id} ({targetClass.Name}) por {username}");

            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_MODIFY_CLASS,
                $"OK|Clase '{targetClass.Name}' modificada exitosamente"
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
}