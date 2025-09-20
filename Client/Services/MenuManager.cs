namespace Client.Services;

/// <summary>
/// Manages all user interface operations including menus and console output
/// </summary>
public class MenuManager
{
    /// <summary>
    /// Shows the menu for users who are not logged in
    /// </summary>
    public void ShowLoggedOutMenu()
    {
        Console.WriteLine("--- Menú ---");
        Console.WriteLine("1. Registrarse");
        Console.WriteLine("2. Iniciar Sesión");
        Console.WriteLine("3. Salir");
        Console.Write("> ");
    }

    /// <summary>
    /// Shows the menu for logged in users
    /// </summary>
    /// <param name="currentUser">The current logged in username</param>
    public void ShowLoggedInMenu(string currentUser)
    {
        Console.WriteLine($"--- Conectado como: {currentUser} ---");
        Console.WriteLine("1. Crear clase");
        Console.WriteLine("2. Ver clases disponibles");
        Console.WriteLine("3. Inscribirse en clase");
        Console.WriteLine("4. Cerrar Sesión (Logout)");
        Console.Write("\n > Seleccione una opción: ");
    }

    /// <summary>
    /// Prompts for class creation data
    /// </summary>
    /// <returns>Class creation data as tuple</returns>
    public (string name, string description, int maxSeats, int duration, DateTime startDateTime, string imagePath) PromptClassCreation()
    {
        Console.Write("Nombre: ");
        string name = Console.ReadLine() ?? "";

        Console.Write("Descripción: ");
        string description = Console.ReadLine() ?? "";

        // Cupos
        int maxSeats;
        while (true)
        {
            Console.Write("Cupos máximos: ");
            string input = Console.ReadLine() ?? "";
            if (int.TryParse(input, out maxSeats) && maxSeats > 0)
                break;
            Console.WriteLine("⚠️ Ingrese un número válido mayor a 0.");
        }

        // Duración
        int duration;
        while (true)
        {
            Console.Write("Duración (minutos): ");
            string input = Console.ReadLine() ?? "";
            if (int.TryParse(input, out duration) && duration > 0)
                break;
            Console.WriteLine("⚠️ Ingrese un número válido mayor a 0.");
        }

        // Fecha
        DateTime startDateTime;
        while (true)
        {
            Console.Write("Fecha y hora (yyyy-MM-dd HH:mm) o vacío para ahora: ");
            string input = Console.ReadLine() ?? "";
            if (string.IsNullOrEmpty(input))
            {
                startDateTime = DateTime.Now;
                break;
            }
            if (DateTime.TryParse(input, out startDateTime))
                break;

            Console.WriteLine("⚠️ Formato de fecha inválido. Ejemplo: 2025-09-15 14:30");
        }

        // Imagen
        Console.Write("Ruta imagen (opcional): ");
        string imagePath = Console.ReadLine() ?? "";

        return (name, description, maxSeats, duration, startDateTime, imagePath);
    }

    /// <summary>
    /// Prompts for user registration data
    /// </summary>
    /// <returns>Username and password as tuple</returns>
    public (string username, string password) PromptRegistration()
    {
        Console.Write("Ingrese nombre de usuario: ");
        string? username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        string? password = Console.ReadLine();

        return (username ?? "", password ?? "");
    }

    /// <summary>
    /// Prompts for user login data
    /// </summary>
    /// <returns>Username and password as tuple</returns>
    public (string username, string password) PromptLogin()
    {
        Console.Write("Ingrese nombre de usuario: ");
        var username = Console.ReadLine();
        Console.Write("Ingrese contraseña: ");
        var password = Console.ReadLine();

        return (username ?? "", password ?? "");
    }

    /// <summary>
    /// Displays a list of available classes
    /// </summary>
    /// <param name="classesData">Formatted string with class information</param>
    public void DisplayClassList(string classesData)
    {
        Console.WriteLine("===== Clases disponibles =====");
        Console.WriteLine(classesData);
    }

    /// <summary>
    /// Shows a success message
    /// </summary>
    /// <param name="message">Success message to display</param>
    public void ShowSuccess(string message)
    {
        Console.WriteLine($"✅ {message}");
    }

    /// <summary>
    /// Shows an error message
    /// </summary>
    /// <param name="message">Error message to display</param>
    public void ShowError(string message)
    {
        Console.WriteLine($"⚠️ {message}");
    }

    /// <summary>
    /// Shows an informational message
    /// </summary>
    /// <param name="message">Message to display</param>
    public void ShowInfo(string message)
    {
        Console.WriteLine($"\n-> {message}");
    }

    /// <summary>
    /// Shows connection status messages
    /// </summary>
    /// <param name="message">Connection message to display</param>
    public void ShowConnectionStatus(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Prompts for class enrollment
    /// </summary>
    /// <returns>Class ID as string</returns>
    public string PromptClassEnrollment()
    {
        Console.Write("Ingrese el ID de la clase a la que desea inscribirse: ");
        return Console.ReadLine() ?? "";
    }

    /// <summary>
    /// Reads a single line of input from console
    /// </summary>
    /// <returns>User input string</returns>
    public string? ReadLine()
    {
        return Console.ReadLine();
    }
}
