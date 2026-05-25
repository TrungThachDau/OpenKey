using Microsoft.Win32;

namespace OpenKey.WinUI.Services;

/// <summary>
/// Updates the current user's Windows startup registration for the WinUI app.
/// </summary>
public sealed class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "OpenKey.WinUI";

    /// <summary>
    /// Enables or disables startup registration for the current executable.
    /// </summary>
    /// <param name="enabled">A value indicating whether startup should be enabled.</param>
    public void SetStartup(bool enabled)
    {
        using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (runKey is null)
        {
            return;
        }

        if (enabled)
        {
            string executablePath = Environment.ProcessPath ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                runKey.SetValue(RunValueName, $"\"{executablePath}\"", RegistryValueKind.String);
            }
        }
        else
        {
            runKey.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
