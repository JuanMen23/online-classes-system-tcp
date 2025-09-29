namespace Common;

public static class PrintMessage
{
    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"⚠️ {message}");
        Console.ResetColor();
    }
    
    public static void Information(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n-> {message}");
        Console.ResetColor();
    }
    
    public static void Success(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ResetColor();
    }
}
