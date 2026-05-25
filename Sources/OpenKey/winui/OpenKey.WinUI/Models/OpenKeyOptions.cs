namespace OpenKey.WinUI.Models;

/// <summary>
/// Stores OpenKey option values compatible with the existing Win32 registry keys.
/// </summary>
public sealed class OpenKeyOptions
{
    /// <summary>
    /// Gets or sets the language mode. A value of 1 enables Vietnamese, and 0 enables English.
    /// </summary>
    public int Language { get; set; } = 1;

    /// <summary>
    /// Gets or sets the input type index.
    /// </summary>
    public int InputType { get; set; }

    /// <summary>
    /// Gets or sets the code table index.
    /// </summary>
    public int CodeTable { get; set; }

    /// <summary>
    /// Gets or sets the packed switch-key configuration.
    /// </summary>
    public int SwitchKeyStatus { get; set; } = 0x7A000206;

    /// <summary>
    /// Gets or sets a value indicating whether spelling checks are enabled.
    /// </summary>
    public bool CheckSpelling { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether modern orthography is enabled.
    /// </summary>
    public bool UseModernOrthography { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether quick Telex is enabled.
    /// </summary>
    public bool QuickTelex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether invalid words restore the original key.
    /// </summary>
    public bool RestoreIfWrongSpelling { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether browser recommendation fixes are enabled.
    /// </summary>
    public bool FixRecommendBrowser { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether macro replacement is enabled.
    /// </summary>
    public bool UseMacro { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether macros are used in English mode.
    /// </summary>
    public bool UseMacroInEnglishMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether macros preserve capitalization.
    /// </summary>
    public bool AutoCapsMacro { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether generated keys are sent one by one.
    /// </summary>
    public bool SendKeyStepByStep { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether smart app-based switching is enabled.
    /// </summary>
    public bool UseSmartSwitchKey { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether first letters are capitalized automatically.
    /// </summary>
    public bool UpperCaseFirstChar { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Z, F, W, and J are accepted as consonants.
    /// </summary>
    public bool AllowConsonantZfwj { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Ctrl temporarily turns off spelling checks.
    /// </summary>
    public bool TempOffSpelling { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether quick initial consonants are enabled.
    /// </summary>
    public bool QuickStartConsonant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether quick ending consonants are enabled.
    /// </summary>
    public bool QuickEndConsonant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the gray tray icon is enabled.
    /// </summary>
    public bool UseGrayIcon { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the app opens its control panel on startup.
    /// </summary>
    public bool ShowOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether OpenKey starts with Windows.
    /// </summary>
    public bool RunWithWindows { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Metro/UWP app support is enabled.
    /// </summary>
    public bool SupportMetroApp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether OpenKey should run as administrator.
    /// </summary>
    public bool RunAsAdmin { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether update checks are enabled.
    /// </summary>
    public bool CheckNewVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether code tables are remembered by app.
    /// </summary>
    public bool RememberCode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether other language input should turn off Vietnamese mode.
    /// </summary>
    public bool OtherLanguage { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Alt temporarily turns off OpenKey.
    /// </summary>
    public bool TempOffOpenKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether Chromium fixes are enabled.
    /// </summary>
    public bool FixChromiumBrowser { get; set; }
}
