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
}
