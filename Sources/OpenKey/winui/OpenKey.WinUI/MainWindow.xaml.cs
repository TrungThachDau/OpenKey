using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpenKey.WinUI.Models;
using OpenKey.WinUI.Services;
using Windows.Graphics;
using WinRT.Interop;

namespace OpenKey.WinUI;

/// <summary>
/// Main settings window for the OpenKey WinUI control panel.
/// </summary>
public sealed partial class MainWindow : Window
{
    private static readonly IReadOnlyList<string> InputTypes = ["Telex", "VNI", "Telex đơn giản"];
    private static readonly IReadOnlyList<string> CodeTables =
    [
        "Unicode",
        "TCVN3 (ABC)",
        "VNI Windows",
        "Unicode Tổ hợp",
        "Vietnamese Locale CP 1258"
    ];

    private static readonly IReadOnlyList<KeyChoice> SwitchKeys =
    [
        new("Không đặt", 0),
        new("A", 0x41),
        new("B", 0x42),
        new("C", 0x43),
        new("D", 0x44),
        new("E", 0x45),
        new("F", 0x46),
        new("G", 0x47),
        new("H", 0x48),
        new("I", 0x49),
        new("J", 0x4A),
        new("K", 0x4B),
        new("L", 0x4C),
        new("M", 0x4D),
        new("N", 0x4E),
        new("O", 0x4F),
        new("P", 0x50),
        new("Q", 0x51),
        new("R", 0x52),
        new("S", 0x53),
        new("T", 0x54),
        new("U", 0x55),
        new("V", 0x56),
        new("W", 0x57),
        new("X", 0x58),
        new("Y", 0x59),
        new("Z", 0x5A),
        new("F1", 0x70),
        new("F2", 0x71),
        new("F3", 0x72),
        new("F4", 0x73),
        new("F5", 0x74),
        new("F6", 0x75),
        new("F7", 0x76),
        new("F8", 0x77),
        new("F9", 0x78),
        new("F10", 0x79),
        new("F11", 0x7A),
        new("F12", 0x7B)
    ];

    private readonly OpenKeyRegistryStore _registryStore = new();
    private readonly StartupRegistrationService _startupRegistrationService = new();
    private readonly OpenKeyUpdateService _updateService = new();
    private readonly OriginalEngineProcessService _engineProcessService = new();
    private OpenKeyOptions _options = new();
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        ApplyMicaBackdrop();
        SetWindowSize();
        InitializeStaticLists();
        SaveControlPanelPath();
        _registryStore.SaveBool("vShowOnStartUp", false);
        LoadOptions();
        StartOriginalEngine();
    }

    private void InitializeStaticLists()
    {
        InputTypeComboBox.ItemsSource = InputTypes;
        CodeTableComboBox.ItemsSource = CodeTables;
        SwitchKeyComboBox.ItemsSource = SwitchKeys;
        SwitchKeyComboBox.DisplayMemberPath = nameof(KeyChoice.Name);
    }

    private void LoadOptions()
    {
        _isLoading = true;
        _options = _registryStore.Load();

        LanguageRadioButtons.SelectedIndex = _options.Language == 1 ? 0 : 1;
        InputTypeComboBox.SelectedIndex = ClampIndex(_options.InputType, InputTypes.Count);
        CodeTableComboBox.SelectedIndex = ClampIndex(_options.CodeTable, CodeTables.Count);
        LoadSwitchKey();

        ModernOrthographySwitch.IsOn = _options.UseModernOrthography;
        FixRecommendBrowserSwitch.IsOn = _options.FixRecommendBrowser;
        CheckSpellingSwitch.IsOn = _options.CheckSpelling;
        RestoreWrongSpellingSwitch.IsOn = _options.RestoreIfWrongSpelling;
        AllowZfwjSwitch.IsOn = _options.AllowConsonantZfwj;
        TempOffSpellingSwitch.IsOn = _options.TempOffSpelling;
        SmartSwitchSwitch.IsOn = _options.UseSmartSwitchKey;
        UpperCaseFirstCharSwitch.IsOn = _options.UpperCaseFirstChar;
        RememberCodeSwitch.IsOn = _options.RememberCode;
        OtherLanguageSwitch.IsOn = _options.OtherLanguage;
        TempOffOpenKeySwitch.IsOn = _options.TempOffOpenKey;

        QuickStartConsonantSwitch.IsOn = _options.QuickStartConsonant;
        QuickEndConsonantSwitch.IsOn = _options.QuickEndConsonant;
        QuickTelexSwitch.IsOn = _options.QuickTelex;
        UseMacroSwitch.IsOn = _options.UseMacro;
        MacroInEnglishSwitch.IsOn = _options.UseMacroInEnglishMode;
        MacroAutoCapsSwitch.IsOn = _options.AutoCapsMacro;

        GrayIconSwitch.IsOn = _options.UseGrayIcon;
        ShowOnStartupSwitch.IsOn = _options.ShowOnStartup;
        RunWithWindowsSwitch.IsOn = _options.RunWithWindows;
        RunAsAdminSwitch.IsOn = _options.RunAsAdmin;
        CheckNewVersionSwitch.IsOn = _options.CheckNewVersion;
        SupportMetroAppSwitch.IsOn = _options.SupportMetroApp;
        UseClipboardSwitch.IsOn = !_options.SendKeyStepByStep;
        FixChromiumSwitch.IsOn = _options.FixChromiumBrowser;

        UpdateSpellingDependencies();
        VersionTextBlock.Text = $"Nền tảng: .NET 10, Windows App SDK. Tiến trình: {Environment.ProcessId}";
        EngineInfoBar.Message = _engineProcessService.IsRunning
            ? $"Engine OpenKey gốc đang chạy: {_engineProcessService.EnginePath}"
            : "Engine OpenKey gốc chưa chạy.";
        EngineInfoBar.IsOpen = true;
        _isLoading = false;
    }

    private void StartOriginalEngine()
    {
        try
        {
            SaveControlPanelPath();
            if (_engineProcessService.IsRunning)
            {
                _engineProcessService.RequestReloadSettings();
            }
            else
            {
                _engineProcessService.EnsureRunning();
            }

            EngineInfoBar.Message = $"Engine OpenKey gốc đang chạy: {_engineProcessService.EnginePath}";
            EngineInfoBar.Severity = InfoBarSeverity.Success;
            EngineInfoBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            EngineInfoBar.Message = $"Không bật được engine gốc: {ex.Message}";
            EngineInfoBar.Severity = InfoBarSeverity.Error;
            EngineInfoBar.IsOpen = true;
        }
    }

    private void LoadSwitchKey()
    {
        int switchKey = _options.SwitchKeyStatus & 0xFF;
        SwitchCtrlCheckBox.IsChecked = (_options.SwitchKeyStatus & 0x100) != 0;
        SwitchAltCheckBox.IsChecked = (_options.SwitchKeyStatus & 0x200) != 0;
        SwitchWinCheckBox.IsChecked = (_options.SwitchKeyStatus & 0x400) != 0;
        SwitchShiftCheckBox.IsChecked = (_options.SwitchKeyStatus & 0x800) != 0;
        SwitchBeepCheckBox.IsChecked = (_options.SwitchKeyStatus & 0x8000) != 0;

        int selectedIndex = SwitchKeys
            .Select((item, index) => new { item.Code, index })
            .FirstOrDefault(item => item.Code == switchKey)?.index ?? 0;
        SwitchKeyComboBox.SelectedIndex = selectedIndex;
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        int language = LanguageRadioButtons.SelectedIndex == 0 ? 1 : 0;
        _options.Language = language;
        _registryStore.SaveInt("vLanguage", language);
        SetOriginalEngineLanguage(language);
    }

    private void OnInputTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || InputTypeComboBox.SelectedIndex < 0)
        {
            return;
        }

        _options.InputType = InputTypeComboBox.SelectedIndex;
        _registryStore.SaveInt("vInputType", _options.InputType);
        ReloadOriginalEngineSettings();
    }

    private void OnCodeTableChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || CodeTableComboBox.SelectedIndex < 0)
        {
            return;
        }

        _options.CodeTable = CodeTableComboBox.SelectedIndex;
        _registryStore.SaveInt("vCodeTable", _options.CodeTable);
        ReloadOriginalEngineSettings();
    }

    private void OnSwitchKeyChanged(object sender, RoutedEventArgs e)
    {
        SaveSwitchKey();
    }

    private void OnSwitchKeyChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveSwitchKey();
    }

    private void SaveSwitchKey()
    {
        if (_isLoading || SwitchKeyComboBox.SelectedItem is not KeyChoice keyChoice)
        {
            return;
        }

        int status = keyChoice.Code;
        if (SwitchCtrlCheckBox.IsChecked == true)
        {
            status |= 0x100;
        }

        if (SwitchAltCheckBox.IsChecked == true)
        {
            status |= 0x200;
        }

        if (SwitchWinCheckBox.IsChecked == true)
        {
            status |= 0x400;
        }

        if (SwitchShiftCheckBox.IsChecked == true)
        {
            status |= 0x800;
        }

        if (SwitchBeepCheckBox.IsChecked == true)
        {
            status |= 0x8000;
        }

        _options.SwitchKeyStatus = status;
        _registryStore.SaveInt("vSwitchKeyStatus", status);
        ReloadOriginalEngineSettings();
    }

    private void OnOptionToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || sender is not ToggleSwitch toggleSwitch || toggleSwitch.Tag is not string registryKey)
        {
            return;
        }

        _registryStore.SaveBool(registryKey, toggleSwitch.IsOn);
        _options = _registryStore.Load();
        UpdateSpellingDependencies();
        ReloadOriginalEngineSettings();
    }

    private void OnUseClipboardToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _registryStore.SaveBool("vSendKeyStepByStep", !UseClipboardSwitch.IsOn);
        _options = _registryStore.Load();
        ReloadOriginalEngineSettings();
    }

    private void OnRunWithWindowsToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _registryStore.SaveBool("vRunWithWindows", RunWithWindowsSwitch.IsOn);
        _startupRegistrationService.SetStartup(RunWithWindowsSwitch.IsOn);
        _options = _registryStore.Load();
        ReloadOriginalEngineSettings();
    }

    private async void OnResetClicked(object sender, RoutedEventArgs e)
    {
        ContentDialog dialog = new()
        {
            Title = "OpenKey",
            Content = "Bạn có chắc chắn muốn thiết lập lại cài đặt gốc?",
            PrimaryButtonText = "Thiết lập lại",
            CloseButtonText = "Hủy",
            XamlRoot = Content.XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        _registryStore.ResetDefaults();
        LoadOptions();
        ReloadOriginalEngineSettings();
        ShowStatus("Đã khôi phục cài đặt mặc định.", InfoBarSeverity.Success);
    }

    private async void OnCheckUpdateClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            string? version = await _updateService.GetLatestWindowsVersionAsync();
            ShowStatus(version is null ? "Không đọc được phiên bản mới nhất." : $"Phiên bản Windows mới nhất: {version}", InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ShowStatus($"Không kiểm tra được cập nhật: {ex.Message}", InfoBarSeverity.Warning);
        }
    }

    private void OnSourceCodeClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/tuyenvm/OpenKey");
    }

    private void OnFanpageClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://www.facebook.com/OpenKeyVN");
    }

    private void OnExitClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSourceCodeTapped(object sender, TappedRoutedEventArgs e)
    {
        OpenUrl("https://github.com/tuyenvm/OpenKey");
        e.Handled = true;
    }

    private void OnCloseTapped(object sender, TappedRoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        if (tag == "Source")
        {
            OpenUrl("https://github.com/tuyenvm/OpenKey");
            return;
        }

        if (tag == "Close")
        {
            Close();
            return;
        }

        GeneralPage.Visibility = tag == "General" ? Visibility.Visible : Visibility.Collapsed;
        MacroPage.Visibility = tag == "Macro" ? Visibility.Visible : Visibility.Collapsed;
        SystemPage.Visibility = tag == "System" ? Visibility.Visible : Visibility.Collapsed;
        AboutPage.Visibility = tag == "About" ? Visibility.Visible : Visibility.Collapsed;
        PageTitleTextBlock.Text = item.Content?.ToString() ?? "OpenKey";
    }

    private void UpdateSpellingDependencies()
    {
        bool enabled = CheckSpellingSwitch.IsOn;
        RestoreWrongSpellingSwitch.IsEnabled = enabled;
        AllowZfwjSwitch.IsEnabled = enabled;
        TempOffSpellingSwitch.IsEnabled = enabled;
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }

    private void ReloadOriginalEngineSettings()
    {
        try
        {
            SaveControlPanelPath();
            if (!_engineProcessService.RequestReloadSettings())
            {
                _engineProcessService.EnsureRunning();
            }

            EngineInfoBar.Message = $"Engine OpenKey gốc đã nạp lại cài đặt: {_engineProcessService.EnginePath}";
            EngineInfoBar.Severity = InfoBarSeverity.Success;
            EngineInfoBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            EngineInfoBar.Message = $"Không reload được engine gốc: {ex.Message}";
            EngineInfoBar.Severity = InfoBarSeverity.Error;
            EngineInfoBar.IsOpen = true;
        }
    }

    private void SetOriginalEngineLanguage(int language)
    {
        try
        {
            SaveControlPanelPath();
            if (!_engineProcessService.RequestSetLanguage(language))
            {
                _engineProcessService.EnsureRunning();
                _engineProcessService.RequestSetLanguage(language);
            }

            EngineInfoBar.Message = language == 1
                ? "Engine OpenKey gốc đã chuyển sang tiếng Việt."
                : "Engine OpenKey gốc đã chuyển sang tiếng Anh.";
            EngineInfoBar.Severity = InfoBarSeverity.Success;
            EngineInfoBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            EngineInfoBar.Message = $"Không đổi được chế độ engine gốc: {ex.Message}";
            EngineInfoBar.Severity = InfoBarSeverity.Error;
            EngineInfoBar.IsOpen = true;
        }
    }

    private void SetWindowSize()
    {
        nint hwnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new SizeInt32(1180, 760));
    }

    private void ApplyMicaBackdrop()
    {
        SystemBackdrop = new MicaBackdrop();
    }

    private static int ClampIndex(int index, int count)
    {
        return index >= 0 && index < count ? index : 0;
    }

    private void SaveControlPanelPath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            _registryStore.SaveString("WinUIControlPanelPath", Environment.ProcessPath);
        }
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private sealed record KeyChoice(string Name, int Code);
}
