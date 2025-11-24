using Common;

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
        Console.WriteLine("2. Modificar clase");
        Console.WriteLine("3. Eliminar clase");
        Console.WriteLine("4. Ver clases disponibles");
        Console.WriteLine("5. Inscribirse en clase");
        Console.WriteLine("6. Cancelar inscripción");
        Console.WriteLine("7. Buscar/filtrar clases");
        Console.WriteLine("8. Ver historial de actividades");
        Console.WriteLine("9. Cerrar Sesión (Logout)");
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
    // Client/Services/MenuManager.cs
    public void DisplayClassList(string classesData)
    {
        Console.WriteLine("\n===== Clases disponibles =====");
        var classLines = classesData.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        if (classLines.Length == 0)
        {
            Console.WriteLine("No hay clases disponibles en este momento.");
        }
        else
        {
            foreach (var line in classLines)
            {
                var parts = line.Split('|');
                if (parts.Length < 8) continue;

                // Format: 0:ID|1:Nombre|2:Desc|3:Fecha|4:Dur|5:Cupos|6:TieneImagen|7:Link
                Console.WriteLine($"ID: {parts[0]} - {parts[1]} ({parts[5]})");
                Console.WriteLine($"   Descripción: {parts[2]}");
                Console.WriteLine($"   Inicio: {parts[3]} ({parts[4]})");
                Console.WriteLine($"   Link: {parts[7]}");

                // --- If it has an image available for download ---
                if (parts[6] == "1")
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($" -> Para descargar la imagen, escribe: descargar {parts[0]}");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
        Console.WriteLine("==============================");
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
    /// Prompts for class enrollment with optional Webhook
    /// </summary>
    /// <returns>Tuple with Class ID and Webhook URL</returns>
    public (string classId, string webhookUrl) PromptClassEnrollment()
    {
        Console.Write("Ingrese el ID de la clase a la que desea inscribirse: ");
        string id = Console.ReadLine() ?? "";
        
        Console.Write("Ingrese URL para notificación (Webhook) [Opcional, ENTER para omitir]: ");
        string url = Console.ReadLine() ?? "";
        
        return (id, url);
    }

    /// <summary>
    /// Prompts for class enrollment cancellation
    /// </summary>
    /// <returns>Class ID as string</returns>
    public string PromptClassCancellation()
    {
        Console.Write("Ingrese el ID de la clase de la cual desea cancelar su inscripción: ");
        return Console.ReadLine() ?? "";
    }

    /// <summary>
    /// Prompts for class modification data
    /// </summary>
    /// <returns>Class ID and modification data as tuple</returns>
    public (string classId, string name, string description, int maxSeats, int duration, DateTime startDateTime, string imagePath) PromptClassModification()
    {
        Console.Write("Ingrese el ID de la clase que desea modificar: ");
        string classId = Console.ReadLine() ?? "";

        Console.WriteLine("Ingrese los nuevos datos para la clase:");
        
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
            PrintMessage.Error("Ingrese un número válido mayor a 0.");
        }

        // Duración
        int duration;
        while (true)
        {
            Console.Write("Duración (minutos): ");
            string input = Console.ReadLine() ?? "";
            if (int.TryParse(input, out duration) && duration > 0)
                break;
            PrintMessage.Error("Ingrese un número válido mayor a 0.");
        }

        // Fecha
        DateTime startDateTime;
        while (true)
        {
            Console.Write("Fecha y hora (yyyy-MM-dd HH:mm): ");
            string input = Console.ReadLine() ?? "";
            if (DateTime.TryParse(input, out startDateTime))
                break;

            PrintMessage.Error("Formato de fecha inválido. Ejemplo: 2025-09-15 14:30");
        }

        // Imagen
        Console.Write("Ruta nueva imagen (opcional, dejar vacío para mantener la actual): ");
        string imagePath = Console.ReadLine() ?? "";

        return (classId, name, description, maxSeats, duration, startDateTime, imagePath);
    }

    /// <summary>
    /// Prompts for class deletion
    /// </summary>
    /// <returns>Class ID as string</returns>
    public string PromptClassDeletion()
    {
        Console.Write("Ingrese el ID de la clase que desea eliminar: ");
        return Console.ReadLine() ?? "";
    }

    /// <summary>
    /// Prompts for search/filter options and returns the formatted data string
    /// </summary>
    /// <returns>Formatted string for protocol (keyword|minDate|maxDate|maxDuration) or null if cancelled</returns>
    public string? PromptSearchClasses()
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
                var keyword = Console.ReadLine() ?? "";
                data = $"{keyword}|||"; // keyword|minDate|maxDate|maxDuration
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
                return null;

            default:
                PrintMessage.Error("Opción inválida.");
                return null;
        }

        return data;
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
