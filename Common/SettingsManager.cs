using System.Configuration;

namespace Common;

public class SettingsManager
{
    public string? ReadSettings(string key)
    {
        try
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[key];
        }
        catch (ConfigurationErrorsException)
        {
            Console.WriteLine("Error leyendo la configuración");
            return null;
        }
    }
}
