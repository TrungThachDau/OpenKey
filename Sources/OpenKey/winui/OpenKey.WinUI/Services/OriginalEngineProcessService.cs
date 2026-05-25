using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenKey.WinUI.Services;

/// <summary>
/// Starts and stops the original Win32 OpenKey engine process built from the source tree.
/// </summary>
public sealed class OriginalEngineProcessService
{
    private const string EngineWindowClass = "OpenKeyVietnameseInputMethod";
    private const int WmUserExit = 0x0400 + 2020;
    private const int WmUserReloadSettings = 0x0400 + 2021;
    private const int WmUserSetLanguage = 0x0400 + 2022;

    /// <summary>
    /// Gets the resolved engine executable path.
    /// </summary>
    public string? EnginePath { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the original engine window is currently available.
    /// </summary>
    public bool IsRunning => FindWindow(EngineWindowClass, null) != IntPtr.Zero;

    /// <summary>
    /// Ensures the original engine process is running.
    /// </summary>
    public void EnsureRunning()
    {
        if (IsRunning)
        {
            return;
        }

        EnginePath ??= ResolveEnginePath();
        if (EnginePath is null)
        {
            throw new FileNotFoundException("Could not find OpenKey64.exe. Build the Win32 OpenKey project first.");
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = EnginePath,
            WorkingDirectory = Path.GetDirectoryName(EnginePath) ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            startInfo.Environment["OPENKEY_WINUI_PATH"] = Environment.ProcessPath;
        }

        Process.Start(startInfo);
    }

    /// <summary>
    /// Restarts the original engine so it reloads registry-backed configuration.
    /// </summary>
    public void Restart()
    {
        RequestExit();
        WaitUntilStopped(TimeSpan.FromSeconds(3));
        EnsureRunning();
    }

    /// <summary>
    /// Requests the original engine to reload registry-backed settings without restarting.
    /// </summary>
    /// <returns><see langword="true"/> when the reload message was sent; otherwise <see langword="false"/>.</returns>
    public bool RequestReloadSettings()
    {
        IntPtr hwnd = FindWindow(EngineWindowClass, null);
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        SendMessage(hwnd, WmUserReloadSettings, IntPtr.Zero, IntPtr.Zero);
        return true;
    }

    /// <summary>
    /// Requests the original engine to switch to English or Vietnamese mode.
    /// </summary>
    /// <param name="language">A value of 1 enables Vietnamese; 0 enables English.</param>
    /// <returns><see langword="true"/> when the message was sent; otherwise <see langword="false"/>.</returns>
    public bool RequestSetLanguage(int language)
    {
        IntPtr hwnd = FindWindow(EngineWindowClass, null);
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        SendMessage(hwnd, WmUserSetLanguage, language == 1 ? 1 : 0, IntPtr.Zero);
        return true;
    }

    /// <summary>
    /// Requests a clean shutdown from the original engine.
    /// </summary>
    public void RequestExit()
    {
        IntPtr hwnd = FindWindow(EngineWindowClass, null);
        if (hwnd != IntPtr.Zero)
        {
            SendMessage(hwnd, WmUserExit, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private void WaitUntilStopped(TimeSpan timeout)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (!IsRunning)
            {
                return;
            }

            Thread.Sleep(100);
        }
    }

    private static string? ResolveEnginePath()
    {
        string[] directCandidates =
        [
            Path.Combine(AppContext.BaseDirectory, "OpenKey64.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "win32", "OpenKey", "OpenKey", "x64", "Release", "OpenKey64.exe")),
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
            string sourceCandidate = Path.Combine(directory.FullName, "Sources", "OpenKey", "win32", "OpenKey", "OpenKey", "x64", "Release", "OpenKey64.exe");
            if (File.Exists(sourceCandidate))
            {
                return sourceCandidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
