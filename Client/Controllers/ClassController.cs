using Common.Protocol;
using Client.Services;

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
    public void CreateClass(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var (name, description, maxSeats, duration, startDateTime, imagePath) = _menuManager.PromptClassCreation();

            if (!_inputValidator.ValidateClassData(name, description, maxSeats, duration))
            {
                _menuManager.ShowError("Datos de clase inválidos. Intente nuevamente.");
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
                    _menuManager.ShowError(ex.Message);
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
            socketService.SendMessage(request);
            _menuManager.ShowInfo("Solicitud de creación de clase enviada.");
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al crear clase: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class list request
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public void RequestClassList(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_LIST_CLASSES,
                "" // no necesitamos data
            );

            setWaitingForResponse(true);
            socketService.SendMessage(request);
            _menuManager.ShowInfo("Solicitud de listado de clases enviada.");
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al solicitar lista de clases: {ex.Message}");
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
    public void EnrollInClass(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var classId = _menuManager.PromptClassEnrollment();

            if (string.IsNullOrEmpty(classId))
            {
                _menuManager.ShowError("ID de clase no puede estar vacío.");
                return;
            }

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_ENROLL_CLASS,
                classId
            );

            setWaitingForResponse(true);
            socketService.SendMessage(request);
            _menuManager.ShowInfo("Solicitud de inscripción enviada.");
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al inscribirse en la clase: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class enrollment cancellation process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public void CancelEnrollment(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var classId = _menuManager.PromptClassCancellation();

            if (string.IsNullOrEmpty(classId))
            {
                _menuManager.ShowError("ID de clase no puede estar vacío.");
                return;
            }

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_CANCEL_ENROLL,
                classId
            );

            setWaitingForResponse(true);
            socketService.SendMessage(request);
            _menuManager.ShowInfo("Solicitud de cancelación de inscripción enviada.");
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al cancelar la inscripción: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class modification process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public void ModifyClass(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var (classId, name, description, maxSeats, duration, startDateTime, imagePath) = _menuManager.PromptClassModification();

            if (string.IsNullOrEmpty(classId))
            {
                _menuManager.ShowError("ID de clase no puede estar vacío.");
                return;
            }

            if (!_inputValidator.ValidateClassData(name, description, maxSeats, duration))
            {
                _menuManager.ShowError("Datos de clase inválidos. Intente nuevamente.");
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
                    _menuManager.ShowError(ex.Message);
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
            socketService.SendMessage(request);
            _menuManager.ShowInfo("Solicitud de modificación de clase enviada.");
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al modificar la clase: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles class deletion process
    /// </summary>
    /// <param name="socketService">Socket service for sending messages</param>
    /// <param name="setWaitingForResponse">Callback to set waiting state</param>
    public void DeleteClass(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        try
        {
            var classId = _menuManager.PromptClassDeletion();

            if (string.IsNullOrEmpty(classId))
            {
                _menuManager.ShowError("ID de clase no puede estar vacío.");
                return;
            }

            // Confirmar eliminación
            Console.Write("¿Está seguro que desea eliminar esta clase? (s/N): ");
            var confirmation = Console.ReadLine()?.ToLower();
            
            if (confirmation != "s" && confirmation != "sí" && confirmation != "si")
            {
                _menuManager.ShowInfo("Operación cancelada.");
                return;
            }

            var request = new ProtocolMessage(
                ProtocolConstants.HEADER_REQUEST,
                ProtocolConstants.CMD_DELETE_CLASS,
                classId
            );

            setWaitingForResponse(true);
            socketService.SendMessage(request);
            _menuManager.ShowInfo("Solicitud de eliminación de clase enviada.");
        }
        catch (Exception ex)
        {
            _menuManager.ShowError($"Error al eliminar la clase: {ex.Message}");
        }
    }
    
    public void SearchClasses(SocketService socketService, Action<bool> setWaitingForResponse)
    {
        Console.WriteLine("=== Buscar/Filtrar Clases ===");
        Console.WriteLine("1. Buscar por palabra clave");
        Console.WriteLine("2. Filtrar por fecha mínima");
        Console.WriteLine("3. Filtrar por fecha máxima");
        Console.WriteLine("4. Filtrar por duración máxima");
        Console.WriteLine("0. Volver al menú principal");
        Console.Write("Seleccione una opción: ");
        string? choice = Console.ReadLine();

        string data = "";

        switch (choice)
        {
            case "1":
                Console.Write("Ingrese palabra clave: ");
                data = Console.ReadLine() ?? "";
                data = $"{data}|||"; // keyword|minDate|maxDate|maxDuration
                break;

            case "2":
                Console.Write("Ingrese fecha mínima (yyyy-MM-dd): ");
                var minDate = Console.ReadLine();
                data = $"|{minDate}||"; // keyword vacío
                break;

            case "3":
                Console.Write("Ingrese fecha máxima (yyyy-MM-dd): ");
                var maxDate = Console.ReadLine();
                data = $"||{maxDate}|"; // keyword y minDate vacíos
                break;

            case "4":
                Console.Write("Ingrese duración máxima (minutos): ");
                var duration = Console.ReadLine();
                data = $"|||{duration}";
                break;

            case "0":
                return;

            default:
                Console.WriteLine("Opción inválida.");
                return;
        }

        var request = new ProtocolMessage(
            ProtocolConstants.HEADER_REQUEST,
            ProtocolConstants.CMD_SEARCH_CLASSES,
            data
        );

        setWaitingForResponse(true);
        socketService.SendMessage(request);
        Console.WriteLine("Solicitud de búsqueda enviada.");
    }

}
