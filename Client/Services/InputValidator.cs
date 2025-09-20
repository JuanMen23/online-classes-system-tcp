namespace Client.Services;

/// <summary>
/// Handles input validation and data parsing operations
/// </summary>
public class InputValidator
{
    private const int MAX_IMAGE_SIZE_MB = 5;
    private const int MAX_IMAGE_SIZE_BYTES = MAX_IMAGE_SIZE_MB * 1024 * 1024;

    /// <summary>
    /// Validates if a username is valid
    /// </summary>
    /// <param name="username">Username to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateUsername(string username)
    {
        return !string.IsNullOrWhiteSpace(username);
    }

    /// <summary>
    /// Validates if a password is valid
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidatePassword(string password)
    {
        return !string.IsNullOrWhiteSpace(password);
    }

    /// <summary>
    /// Validates user credentials (username and password)
    /// </summary>
    /// <param name="username">Username to validate</param>
    /// <param name="password">Password to validate</param>
    /// <returns>True if both are valid, false otherwise</returns>
    public bool ValidateCredentials(string username, string password)
    {
        return ValidateUsername(username) && ValidatePassword(password);
    }

    /// <summary>
    /// Validates class creation data
    /// </summary>
    /// <param name="name">Class name</param>
    /// <param name="description">Class description</param>
    /// <param name="maxSeats">Maximum seats</param>
    /// <param name="duration">Class duration in minutes</param>
    /// <returns>True if all data is valid, false otherwise</returns>
    public bool ValidateClassData(string name, string description, int maxSeats, int duration)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               !string.IsNullOrWhiteSpace(description) &&
               maxSeats > 0 &&
               duration > 0;
    }

    /// <summary>
    /// Parses a date time string with fallback to current time
    /// </summary>
    /// <param name="input">Date time input string</param>
    /// <returns>Parsed DateTime or current time if input is empty</returns>
    public DateTime ParseDateTime(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return DateTime.Now;
        }

        if (DateTime.TryParse(input, out DateTime result))
        {
            return result;
        }

        throw new ArgumentException("Formato de fecha inválido");
    }

    /// <summary>
    /// Reads and validates an image file, converting it to base64
    /// </summary>
    /// <param name="imagePath">Path to the image file</param>
    /// <returns>Base64 encoded image string or empty string if invalid</returns>
    public string ReadImageFile(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
        {
            return "";
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(imagePath);

            if (bytes.Length > MAX_IMAGE_SIZE_BYTES)
            {
                throw new InvalidOperationException($"Imagen demasiado grande (máximo {MAX_IMAGE_SIZE_MB}MB)");
            }

            return Convert.ToBase64String(bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error leyendo la imagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates if a positive integer can be parsed from input
    /// </summary>
    /// <param name="input">Input string to validate</param>
    /// <param name="value">Parsed integer value if valid</param>
    /// <returns>True if valid positive integer, false otherwise</returns>
    public bool TryParsePositiveInt(string input, out int value)
    {
        value = 0;
        return int.TryParse(input, out value) && value > 0;
    }

    /// <summary>
    /// Validates if a valid integer can be parsed from input
    /// </summary>
    /// <param name="input">Input string to validate</param>
    /// <param name="value">Parsed integer value if valid</param>
    /// <returns>True if valid integer, false otherwise</returns>
    public bool TryParseInt(string input, out int value)
    {
        return int.TryParse(input, out value);
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
        return $"{name}|{description}|{maxSeats}|{startDateTime:yyyy-MM-dd HH:mm}|{duration}|{imageBase64}";
    }

    /// <summary>
    /// Formats user credentials for protocol transmission
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>Formatted string for protocol</returns>
    public string FormatCredentials(string username, string password)
    {
        return $"{username}|{password}";
    }
}
