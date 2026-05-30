using System.Runtime.InteropServices;

namespace OpenKey.WinUI.Services;

/// <summary>
/// Hosts the OpenKey native keyboard engine in the WinUI process through OpenKeyNative.dll.
/// </summary>
public sealed class NativeEngineService
{
    /// <summary>
    /// Gets the resolved native DLL path.
    /// </summary>
    public string? EnginePath { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the native keyboard hook is active.
    /// </summary>
    public bool IsRunning => NativeMethods.IsHookRunning() == 1;

    /// <summary>
    /// Initializes the native engine and starts the low-level keyboard hook.
    /// </summary>
    public void EnsureRunning()
    {
        EnginePath ??= ResolveNativeEnginePath();
        if (EnginePath is null)
        {
            throw new FileNotFoundException("Could not find OpenKeyNative.dll. Build the OpenKeyNative project first.");
        }

        if (NativeMethods.Initialize() != 1 || NativeMethods.StartHook() != 1)
        {
            throw new InvalidOperationException("OpenKeyNative.dll could not start the keyboard hook.");
        }
    }

    /// <summary>
    /// Restarts the native hook after registry-backed configuration changes.
    /// </summary>
    public void Restart()
    {
        NativeMethods.StopHook();
        NativeMethods.ReloadSettings();
        EnsureRunning();
    }

    /// <summary>
    /// Reloads settings in the native engine.
    /// </summary>
    public bool RequestReloadSettings()
    {
        return NativeMethods.ReloadSettings() == 1;
    }

    /// <summary>
    /// Switches the native engine to English or Vietnamese mode.
    /// </summary>
    /// <param name="language">A value of 1 enables Vietnamese; 0 enables English.</param>
    public bool RequestSetLanguage(int language)
    {
        NativeMethods.SetLanguage(language == 1 ? 1 : 0);
        return true;
    }

    /// <summary>
    /// Gets the current language from the native engine.
    /// </summary>
    public int GetLanguage()
    {
        return NativeMethods.GetLanguage();
    }

    /// <summary>
    /// Stops the native hook.
    /// </summary>
    public void RequestExit()
    {
        NativeMethods.StopHook();
    }

    private static string? ResolveNativeEnginePath()
    {
        string[] directCandidates =
        [
            Path.Combine(AppContext.BaseDirectory, "OpenKeyNative.dll"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "OpenKeyNative", "x64", "Release", "OpenKeyNative.dll")),
        ];

        foreach (string candidate in directCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string sourceCandidate = Path.Combine(directory.FullName, "Sources", "OpenKey", "winui", "OpenKeyNative", "x64", "Release", "OpenKeyNative.dll");
            if (File.Exists(sourceCandidate))
            {
                return sourceCandidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static class NativeMethods
    {
        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_Initialize")]
        internal static extern int Initialize();

        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_StartHook")]
        internal static extern int StartHook();

        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_StopHook")]
        internal static extern void StopHook();

        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_ReloadSettings")]
        internal static extern int ReloadSettings();

        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_SetLanguage")]
        internal static extern void SetLanguage(int language);

        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_GetLanguage")]
        internal static extern int GetLanguage();

        [DllImport("OpenKeyNative.dll", EntryPoint = "OpenKeyNative_IsHookRunning")]
        internal static extern int IsHookRunning();
    }
}
