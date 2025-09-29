using System.Reflection;

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

    private readonly ConcurrentDictionary<int, ClassSession> _classes = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _classLocks = new();
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

        _classes.TryAdd(id, newClass);
        return newClass;
    }
    
    public ClassSession CreateClassWithDetails(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imageBase64, string createdBy)
    {
        string? imagePath = null;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            try
            {
                byte[] imageBytes = Convert.FromBase64String(imageBase64);
                
                // Creates the Image folder on the Server if it doesn't exist
                Directory.CreateDirectory("Images"); 
                
                string fileName = $"{Guid.NewGuid()}.png";
                imagePath = Path.Combine("Images", fileName);
                File.WriteAllBytes(imagePath, imageBytes);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Formato de imagen Base64 inválido");
            }
        }
        
        var createdClass = CreateClass(name, description, maxSeats, startDateTime, durationMinutes, imagePath, createdBy);
        
        Console.WriteLine($"Clase creada: {createdClass.Id} ({createdClass.Name}) por {createdBy}");
        return createdClass;
    }

    public IEnumerable<ClassSession> GetAllClasses() => _classes.Values.ToList();

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
            // --- If the Class has an image: 1, otherwise 0 ---
            string hasImageFlag = string.IsNullOrEmpty(c.ImagePath) ? "0" : "1";
            int enrolled = c.EnrolledCount;

            sb.AppendLine($"{c.Id}|{c.Name}|{c.Description}|" +
                          $"{c.StartDateTime:yyyy-MM-dd HH:mm}|{c.DurationMinutes} min|" +
                          $"{enrolled}/{c.MaxSeats}|{hasImageFlag}");
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
            if (!_classes.TryGetValue(classId, out var targetClass))
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

            // Obtener semáforo para esta clase específica (locking granular)
            var semaphore = _classLocks.GetOrAdd(classId, _ => new SemaphoreSlim(1, 1));
            
            semaphore.Wait();
            try
            {
                // Verificar si ya está inscrito (re-verificar dentro del lock)
                if (targetClass.IsEnrolled(username))
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        "Ya estás inscrito en esta clase"
                    );
                }

                // Verificar si hay cupos disponibles (re-verificar dentro del lock)
                if (targetClass.EnrolledCount >= targetClass.MaxSeats)
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        "No hay cupos disponibles para esta clase"
                    );
                }

                // Inscribir al usuario de forma atómica
                targetClass.EnrollUser(username);

                Console.WriteLine($"Usuario '{username}' inscrito en clase '{targetClass.Name}' (ID: {targetClass.Id})");

                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ENROLL_CLASS,
                    $"OK|Inscripción exitosa en la clase '{targetClass.Name}'"
                );
            }
            finally
            {
                semaphore.Release();
            }
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
            if (!_classes.TryGetValue(classId, out var targetClass))
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

            // Obtener semáforo para esta clase específica (locking granular)
            var semaphore = _classLocks.GetOrAdd(classId, _ => new SemaphoreSlim(1, 1));
            
            semaphore.Wait();
            try
            {
                // Verificar si está inscrito en la clase (re-verificar dentro del lock)
                if (!targetClass.IsEnrolled(username))
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

                // Cancelar la inscripción de forma atómica
                if (!targetClass.RemoveUser(username))
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        "Error al cancelar la inscripción"
                    );
                }

                Console.WriteLine($"Usuario '{username}' canceló su inscripción en la clase '{targetClass.Name}' (ID: {targetClass.Id})");

                return new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_CANCEL_ENROLL,
                    $"OK|Inscripción cancelada exitosamente en la clase '{targetClass.Name}'. El cupo queda disponible para otros usuarios."
                );
            }
            finally
            {
                semaphore.Release();
            }
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
            if (!_classes.TryGetValue(classId, out var targetClass))
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

            // Obtener semáforo para esta clase específica
            var semaphore = _classLocks.GetOrAdd(classId, _ => new SemaphoreSlim(1, 1));
            
            semaphore.Wait();
            try
            {
                // Re-verificar que la clase aún existe dentro del lock
                if (!_classes.TryGetValue(classId, out targetClass))
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        "Clase no encontrada"
                    );
                }

                // Verificar que el usuario sea el creador de la clase (re-verificar dentro del lock)
                if (targetClass.CreatedBy != username)
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        "Solo el creador de la clase puede modificarla"
                    );
                }

                // Verificar que la clase no haya comenzado (re-verificar dentro del lock)
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
                if (newMaxSeats < targetClass.EnrolledCount)
                {
                    return new ProtocolMessage(
                        ProtocolConstants.HEADER_RESPONSE,
                        ProtocolConstants.CMD_ERROR,
                        $"No se puede reducir los cupos por debajo del número de usuarios inscritos ({targetClass.EnrolledCount})"
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
            finally
            {
                semaphore.Release();
            }
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

    public ProtocolMessage HandleDeleteClass(string data, Guid clientId)
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

    ProtocolMessage responseMessage;

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
        if (!_classes.TryGetValue(classId, out var targetClass))
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

        // Obtener semáforo para esta clase específica (locking granular)
        var semaphore = _classLocks.GetOrAdd(classId, _ => new SemaphoreSlim(1, 1));

        // Flags para limpieza posterior
        bool removed = false;
        ClassSession removedClass = null;

        semaphore.Wait();
        try
        {
            // Re-verificar que la clase aún existe dentro del lock
            if (!_classes.TryGetValue(classId, out targetClass))
            {
                responseMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Clase no encontrada"
                );
                return responseMessage;
            }

            // Verificar que el usuario sea el creador de la clase (re-verificar dentro del lock)
            if (targetClass.CreatedBy != username)
            {
                responseMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Solo el creador de la clase puede eliminarla"
                );
                return responseMessage;
            }

            // Verificar que no haya usuarios inscritos (re-verificar dentro del lock)
            if (targetClass.EnrolledCount > 0)
            {
                responseMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se puede eliminar una clase que tiene usuarios inscritos"
                );
                return responseMessage;
            }

            // Verificar que la clase no haya comenzado (re-verificar dentro del lock)
            if (targetClass.StartDateTime <= DateTime.Now)
            {
                responseMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "No se puede eliminar una clase que ya ha comenzado"
                );
                return responseMessage;
            }

            // Intentar eliminar la clase
            if (_classes.TryRemove(classId, out removedClass))
            {
                // Eliminar imagen asociada si existe
                if (!string.IsNullOrEmpty(removedClass.ImagePath) && File.Exists(removedClass.ImagePath))
                {
                    try
                    {
                        File.Delete(removedClass.ImagePath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Advertencia: No se pudo eliminar la imagen {removedClass.ImagePath}: {ex.Message}");
                    }
                }

                // marcar que se eliminó (limpieza del semáforo se hace *después* del finally)
                removed = true;

                Console.WriteLine($"Clase eliminada: {removedClass.Id} ({removedClass.Name}) por {username}");

                responseMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_DELETE_CLASS,
                    $"OK|Clase '{removedClass.Name}' eliminada exitosamente"
                );
            }
            else
            {
                responseMessage = new ProtocolMessage(
                    ProtocolConstants.HEADER_RESPONSE,
                    ProtocolConstants.CMD_ERROR,
                    "Error al eliminar la clase"
                );
            }
        }
        finally
        {
            // Always release the semaphore we waited on
            try
            {
                semaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // Shouldn't happen with the new ordering, but guard just in case
            }
        }

        // Ahora que liberamos el semáforo, podemos removerlo y disponerlo si corresponde
        if (removed)
        {
            if (_classLocks.TryRemove(classId, out var semaphoreToDispose))
            {
                try
                {
                    semaphoreToDispose.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Advertencia: no se pudo dispose del semáforo: {ex.Message}");
                }
            }
        }

        return responseMessage;
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
    
    public ProtocolMessage HandleSearchClasses(string data)
    {
        try
        {
            var parts = data.Split('|');
            string keyword = parts.Length > 0 ? parts[0] : "";
            string minDateStr = parts.Length > 1 ? parts[1] : "";
            string maxDateStr = parts.Length > 2 ? parts[2] : "";
            string maxDurStr = parts.Length > 3 ? parts[3] : "";

            DateTime? minDate = DateTime.TryParse(minDateStr, out var d1) ? d1 : null;
            DateTime? maxDate = DateTime.TryParse(maxDateStr, out var d2) ? d2 : null;
            int? maxDuration = int.TryParse(maxDurStr, out var dur) ? dur : null;

            //  usar Values para obtener directamente los objetos ClassSession
            var filtered = _classes.Values.Where(c =>
                (string.IsNullOrEmpty(keyword) ||
                 c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                 c.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)) &&
                (!minDate.HasValue || c.StartDateTime.Date >= minDate.Value.Date) &&
                (!maxDate.HasValue || c.StartDateTime.Date <= maxDate.Value.Date) &&
                (!maxDuration.HasValue || c.DurationMinutes <= maxDuration.Value)
            );

            var sb = new StringBuilder();
            foreach (var c in filtered)
            {
                string hasImage = string.IsNullOrEmpty(c.ImagePath) ? "No" : "Sí";
                int enrolled = c.EnrolledCount;
                sb.AppendLine($"{c.Id} | {c.Name} | {c.Description} | " +
                              $"{c.StartDateTime:yyyy-MM-dd HH:mm} | {c.DurationMinutes} min | " +
                              $"Cupos: {enrolled}/{c.MaxSeats} | Imagen: {hasImage}");
            }

            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_SEARCH_CLASSES,
                sb.Length > 0 ? sb.ToString() : "No se encontraron clases con esos filtros."
            );
        }
        catch (Exception ex)
        {
            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ERROR,
                $"Error al buscar clases: {ex.Message}"
            );
        }
    }
    
    public ProtocolMessage HandleHistory(Guid clientId)
    {
        if (!UserService.Instance.IsUserLoggedIn(clientId))
        {
            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ERROR,
                "Debes iniciar sesión primero."
            );
        }

        var username = UserService.Instance.GetLoggedInUsername(clientId);
        if (string.IsNullOrEmpty(username))
        {
            return new ProtocolMessage(
                ProtocolConstants.HEADER_RESPONSE,
                ProtocolConstants.CMD_ERROR,
                "No se pudo obtener el usuario actual."
            );
        }

        var sb = new StringBuilder();
        foreach (var c in _classes.Values)
        {
            foreach (var e in c.Enrollments.Where(e => e.Username == username))
            {
                string estado;
                if (e.IsCancelled) estado = "Cancelada";
                else if (c.StartDateTime < DateTime.Now) estado = "Finalizada";
                else estado = "Activa";

                sb.AppendLine($"{c.Id} | {c.Name} | {c.StartDateTime:yyyy-MM-dd HH:mm} | Estado: {estado}");
            }
        }

        return new ProtocolMessage(
            ProtocolConstants.HEADER_RESPONSE,
            ProtocolConstants.CMD_HISTORY,
            sb.Length > 0 ? sb.ToString() : "No tienes inscripciones registradas."
        );
    }
    
    public string GetClassImageAsBase64(int classId)
    {
        // 1. Search for the class
        if (!_classes.TryGetValue(classId, out var targetClass))
        {
            throw new ArgumentException("Clase no encontrada");
        }

        // 2. Verify if the class has an image associated
        if (string.IsNullOrEmpty(targetClass.ImagePath))
        {
            throw new InvalidOperationException("Esta clase no tiene una imagen asociada.");
        }
        
        // 3. Read the file and convert it to Base64
        byte[]? imageBytes = null;
        
        // Try to read as embedded resource first (for old images)
        if (targetClass.ImagePath.StartsWith("Server.Images."))
        {
            imageBytes = ReadEmbeddedResource(targetClass.ImagePath);
        }
        // Try to read as file path (for new images)
        else if (File.Exists(targetClass.ImagePath))
        {
            imageBytes = File.ReadAllBytes(targetClass.ImagePath);
        }

        if (imageBytes == null)
        {
            throw new FileNotFoundException($"El archivo de la imagen '{targetClass.ImagePath}' no se encontró en el servidor.");
        }
        return Convert.ToBase64String(imageBytes);
    }
    
    public void CreateClassFromData(
        int id, string name, string description, int maxSeats, DateTime startDateTime, 
        int durationMinutes, string? imagePath, string createdBy, List<string> enrolledUsers
        )
    {
        var newClass = new ClassSession
        {
            Id = id,
            Name = name,
            Description = description,
            MaxSeats = maxSeats,
            StartDateTime = startDateTime,
            DurationMinutes = durationMinutes,
            ImagePath = imagePath,
            CreatedBy = createdBy,
            Link = $"class-{Guid.NewGuid()}"
        };
    
        foreach(var user in enrolledUsers)
        {
            newClass.EnrollUser(user);
        }

        _classes.TryAdd(id, newClass);
    }
    
    public void SetNextId(int id)
    {
        lock (_lockNextId)
        {
            _nextId = id;
        }
    }
    
    private byte[]? ReadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null) return null;

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
