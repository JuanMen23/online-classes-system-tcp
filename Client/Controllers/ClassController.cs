using Common.Protocol;
using Client.Services;
using Common;

namespace Client.Controllers;

/// <summary>
/// Handles all class-related operations including creation, listing, and management
/// </summary>
public class ClassController
{
    private readonly MenuManager _menuManager;
    private readonly InputValidator _inputValidator;

    public ClassController(MenuManager menuManager, InputValidator inputValidator)
    {
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
    }

    /// <summary>
    /// Handles class creation process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task CreateClassAsync(SocketService socketService, Action<bool> setWaitingForResponse)
{
    try
    {
        var (name, description, maxSeats, duration, startDateTime, imagePath) = _menuManager.PromptClassCreation();
        
        if (!_inputValidator.ValidateClassData(name, description, maxSeats, duration))
        {
            PrintMessage.Error("Datos de clase inválidos. Intente nuevamente.");
            return;
        }
        
        string imageBase64 = ""; 
        if (!string.IsNullOrEmpty(imagePath))
        {
            try
            {
                string extension = Path.GetExtension(imagePath).ToLower();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                {
                    PrintMessage.Error("El archivo debe ser una imagen (.jpg, .jpeg, .png).");
                    return;
                }
                
                imageBase64 = _inputValidator.ReadImageFile(imagePath);
                PrintMessage.Success("Imagen procesada exitosamente.");
            }
            catch (InvalidOperationException ex)
            {
                PrintMessage.Error(ex.Message);
                return;
            }
        }
        
        string data = _inputValidator.FormatClassData(name, description, maxSeats, startDateTime, duration, imageBase64);

        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_CREATE_CLASS,
            data
        );

        setWaitingForResponse(true);
        await socketService.SendMessageAsync(request);
        PrintMessage.Information("Solicitud de creación de clase enviada.");
    }
    catch (Exception ex)
    {
        PrintMessage.Error($"Error al crear clase: {ex.Message}");
    }
}

    /// <summary>
    /// Handles class list request
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task RequestClassListAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_LIST_CLASSES,
                "" // no necesitamos data
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(request);
            PrintMessage.Information("Solicitud de listado de clases enviada.");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al solicitar lista de clases: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates class creation data
    /// </summary>
    /// <param name="name">Class name</param>
    /// <param name="description">Class description</param>
    /// <param name="maxSeats">Maximum seats</param>
    /// <param name="duration">Class duration in minutes</param>
    /// <returns>True if data is valid, false otherwise</returns>
    public bool ValidateClassData(string name, string description, int maxSeats, int duration)
    {
        return _inputValidator.ValidateClassData(name, description, maxSeats, duration);
    }

    /// <summary>
    /// Reads and validates an image file
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>Base64 encoded image string</returns>
    /// <exception cref="InvalidOperationException">Thrown when image is invalid or too large</exception>
    public string ReadImageFile(string imagePath)
    {
        return _inputValidator.ReadImageFile(imagePath);
    }

    /// <summary>
    /// Formats class data for protocol transmission
    /// </summary>
    /// <param name="name">Class name</param>
    /// <param name="description">Class description</param>
    /// <param name="maxSeats">Maximum seats</param>
    /// <param name="startDateTime">Class start date and time</param>
    /// <param name="duration">Class duration in minutes</param>
    /// <param name="imageBase64">Base64 encoded image</param>
    /// <returns>Formatted string for protocol</returns>
    public string FormatClassData(string name, string description, int maxSeats, DateTime startDateTime, int duration, string imageBase64)
    {
        return _inputValidator.FormatClassData(name, description, maxSeats, startDateTime, duration, imageBase64);
    }

    /// <summary>
    /// Handles class enrollment process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task EnrollInClassAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var (classId, webhookUrl) = _menuManager.PromptClassEnrollment();

            if (string.IsNullOrEmpty(classId))
            {
                PrintMessage.Error("ID de clase no puede estar vacío.");
                return;
            }
            
            string data = string.IsNullOrWhiteSpace(webhookUrl) 
                ? classId 
                : $"{classId}|{webhookUrl}";

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_ENROLL_CLASS,
                data
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(request);
            PrintMessage.Information("Solicitud de inscripción enviada.");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al inscribirse en la clase: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class enrollment cancellation process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task CancelEnrollmentAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var classId = _menuManager.PromptClassCancellation();

            if (string.IsNullOrEmpty(classId))
            {
                PrintMessage.Error("ID de clase no puede estar vacío.");
                return;
            }

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_CANCEL_ENROLL,
                classId
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(request);
            PrintMessage.Information("Solicitud de cancelación de inscripción enviada.");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al cancelar la inscripción: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class modification process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task ModifyClassAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var (classId, name, description, maxSeats, duration, startDateTime, imagePath) = _menuManager.PromptClassModification();

            if (string.IsNullOrEmpty(classId))
            {
                PrintMessage.Error("ID de clase no puede estar vacío.");
                return;
            }

            if (!_inputValidator.ValidateClassData(name, description, maxSeats, duration))
            {
                PrintMessage.Error("Datos de clase inválidos. Intente nuevamente.");
                return;
            }

            string imageBase64 = "";
            if (!string.IsNullOrEmpty(imagePath))
            {
                try
                {
                    imageBase64 = _inputValidator.ReadImageFile(imagePath);
                }
                catch (InvalidOperationException ex)
                {
                    PrintMessage.Error(ex.Message);
                    return;
                }
            }

            var data = $"{classId}|{name}|{description}|{maxSeats}|{startDateTime:yyyy-MM-dd HH:mm}|{duration}|{imageBase64}";

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_MODIFY_CLASS,
                data
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(request);
            PrintMessage.Information("Solicitud de modificación de clase enviada.");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al modificar la clase: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class deletion process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public async Task DeleteClassAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var classId = _menuManager.PromptClassDeletion();

            if (string.IsNullOrEmpty(classId))
            {
                PrintMessage.Error("ID de clase no puede estar vacío.");
                return;
            }

            // Confirmar eliminación
            Console.Write("¿Está seguro que desea eliminar esta clase? (s/N): ");
            var confirmation = Console.ReadLine()?.ToLower();
            
            if (confirmation != "s" && confirmation != "sí" && confirmation != "si")
            {
                PrintMessage.Information("Operación cancelada.");
                return;
            }

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_DELETE_CLASS,
                classId
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(request);
            PrintMessage.Information("Solicitud de eliminación de clase enviada.");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al eliminar la clase: {ex.Message}");
        }
    }
    
    public async Task SearchClassesAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        var data = _menuManager.PromptSearchClasses();
        if (data == null) return; // el usuario canceló

        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_SEARCH_CLASSES,
            data
        );

        setWaitingForResponse(true);
        await socketService.SendMessageAsync(request);
        PrintMessage.Information("Solicitud de búsqueda enviada.");
    }
    
    public async Task RequestHistoryAsync(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_HISTORY,
            ""
        );

        setWaitingForResponse(true);
        await socketService.SendMessageAsync(request);
        PrintMessage.Information("Solicitud de historial enviada.");
    }
    public async Task DownloadImageAsync(SocketService socketService, Action<bool> setWaitingForResponse, string classId)
    {
        try
        {
            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_DOWNLOAD_IMAGE,
                classId
            );

            setWaitingForResponse(true);
            await socketService.SendMessageAsync(request);
            PrintMessage.Information($"Solicitando imagen para la clase {classId}...");
        }
        catch (Exception ex)
        {
            PrintMessage.Error($"Error al solicitar la imagen: {ex.Message}");
        }
    }
}
