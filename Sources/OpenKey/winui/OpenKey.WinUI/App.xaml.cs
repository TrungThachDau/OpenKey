using Microsoft.UI.Xaml;

namespace OpenKey.WinUI;

/// <summary>
/// Coordinates application startup and owns the main WinUI window.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the current main application window.
    /// </summary>
    public static Window? MainWindow { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
