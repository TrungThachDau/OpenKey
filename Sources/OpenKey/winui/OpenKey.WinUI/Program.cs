using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices;
using WinRT;

namespace OpenKey.WinUI;

/// <summary>
/// Hosts the WinUI application entry point.
/// </summary>
public static class Program
{
    private const string SingleInstanceMutexName = "OpenKey.WinUI.SingleInstance";
    private static Mutex? _singleInstanceMutex;

    /// <summary>
    /// Starts the OpenKey WinUI desktop application.
    /// </summary>
    /// <param name="args">The command-line arguments supplied by Windows.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        if (!createdNew)
        {
            BringExistingWindowToFront();
            return;
        }

        ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static void BringExistingWindowToFront()
    {
        nint hwnd = FindWindow(null, "Cài đặt OpenKey");
        if (hwnd == 0)
        {
            return;
        }

        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hWnd);
}
