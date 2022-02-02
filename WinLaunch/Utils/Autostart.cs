using Microsoft.Win32;

/// <summary>
/// Autostart Utility.
/// </summary>
public class Autostart
{
    private const string RUN_LOCATION = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Sets the autostart value for the assembly.
    /// </summary>
    /// <param name="keyName">Registry Key Name</param>
    /// <param name="assemblyLocation">Assembly location (e.g. Assembly.GetExecutingAssembly().Location)</param>
    public static void SetAutoStart(string keyName, string assemblyLocation, string args)
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
            key.SetValue(keyName, assemblyLocation + args);
        }
        catch { }
    }

    /// <summary>
    /// Returns whether auto start is enabled.
    /// </summary>
    /// <param name="keyName">Registry Key Name</param>
    /// <param name="assemblyLocation">Assembly location (e.g. Assembly.GetExecutingAssembly().Location)</param>
    public static bool IsAutoStartEnabled(string keyName, string assemblyLocation, string args)
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION);
            if (key == null)
                return false;

            string value = (string)key.GetValue(keyName);
            if (value == null)
                return false;

            return (value == assemblyLocation + args);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Unsets the autostart value for the assembly.
    /// </summary>
    /// <param name="keyName">Registry Key Name</param>
    public static void UnsetAutoStart(string keyName)
    {
        try
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
            key.DeleteValue(keyName);
        }
        catch { }
    }
}