using Microsoft.Win32;
using OpenKey.WinUI.Models;

namespace OpenKey.WinUI.Services;

/// <summary>
/// Reads and writes OpenKey settings in the same registry location used by the Win32 source.
/// </summary>
public sealed class OpenKeyRegistryStore
{
    private const string RegistryPath = @"SOFTWARE\TuyenMai\OpenKey";

    /// <summary>
    /// Loads all known OpenKey options from the current user's registry hive.
    /// </summary>
    /// <returns>The loaded options, with source-compatible defaults when values are missing.</returns>
    public OpenKeyOptions Load()
    {
        return new OpenKeyOptions
        {
            Language = GetInt("vLanguage", 1),
            InputType = GetInt("vInputType", 0),
            CodeTable = GetInt("vCodeTable", 0),
            SwitchKeyStatus = GetInt("vSwitchKeyStatus", 0x7A000206),
            CheckSpelling = GetBool("vCheckSpelling", true),
            UseModernOrthography = GetBool("vUseModernOrthography", false),
            QuickTelex = GetBool("vQuickTelex", false),
            RestoreIfWrongSpelling = GetBool("vRestoreIfWrongSpelling", true),
            FixRecommendBrowser = GetBool("vFixRecommendBrowser", true),
            UseMacro = GetBool("vUseMacro", true),
            UseMacroInEnglishMode = GetBool("vUseMacroInEnglishMode", false),
            AutoCapsMacro = GetBool("vAutoCapsMacro", false),
            SendKeyStepByStep = GetBool("vSendKeyStepByStep", true),
            UseSmartSwitchKey = GetBool("vUseSmartSwitchKey", true),
            UpperCaseFirstChar = GetBool("vUpperCaseFirstChar", false),
            AllowConsonantZfwj = GetBool("vAllowConsonantZFWJ", false),
            TempOffSpelling = GetBool("vTempOffSpelling", false),
            QuickStartConsonant = GetBool("vQuickStartConsonant", false),
            QuickEndConsonant = GetBool("vQuickEndConsonant", false),
            UseGrayIcon = GetBool("vUseGrayIcon", false),
            ShowOnStartup = GetBool("vShowOnStartUp", true),
            RunWithWindows = GetBool("vRunWithWindows", true),
            SupportMetroApp = GetBool("vSupportMetroApp", false),
            RunAsAdmin = GetBool("vRunAsAdmin", false),
            CheckNewVersion = GetBool("vCheckNewVersion", false),
            RememberCode = GetBool("vRememberCode", true),
            OtherLanguage = GetBool("vOtherLanguage", true),
            TempOffOpenKey = GetBool("vTempOffOpenKey", false),
            FixChromiumBrowser = GetBool("vFixChromiumBrowser", false),
        };
    }

    /// <summary>
    /// Persists a single integer option value.
    /// </summary>
    /// <param name="key">The registry value name.</param>
    /// <param name="value">The integer value to persist.</param>
    public void SaveInt(string key, int value)
    {
        using RegistryKey registryKey = OpenWritableKey();
        registryKey.SetValue(key, value, RegistryValueKind.DWord);
    }

    /// <summary>
    /// Persists a single Boolean option value as a DWORD.
    /// </summary>
    /// <param name="key">The registry value name.</param>
    /// <param name="value">The Boolean value to persist.</param>
    public void SaveBool(string key, bool value)
    {
        SaveInt(key, value ? 1 : 0);
    }

    /// <summary>
    /// Persists a single string option value.
    /// </summary>
    /// <param name="key">The registry value name.</param>
    /// <param name="value">The string value to persist.</param>
    public void SaveString(string key, string value)
    {
        using RegistryKey registryKey = OpenWritableKey();
        registryKey.SetValue(key, value, RegistryValueKind.String);
    }

    /// <summary>
    /// Resets the registry-backed options to the defaults used by the Win32 implementation.
    /// </summary>
    public void ResetDefaults()
    {
        SaveInt("vLanguage", 1);
        SaveInt("vInputType", 0);
        SaveInt("vCodeTable", 0);
        SaveInt("vCheckSpelling", 1);
        SaveInt("vUseModernOrthography", 0);
        SaveInt("vQuickTelex", 0);
        SaveInt("vSwitchKeyStatus", 0x7A000206);
        SaveInt("vRestoreIfWrongSpelling", 1);
        SaveInt("vFixRecommendBrowser", 1);
        SaveInt("vUseMacro", 0);
        SaveInt("vUseMacroInEnglishMode", 0);
        SaveInt("vSendKeyStepByStep", 1);
        SaveInt("vUseSmartSwitchKey", 1);
        SaveInt("vUpperCaseFirstChar", 0);
        SaveInt("vAllowConsonantZFWJ", 0);
        SaveInt("vTempOffSpelling", 0);
        SaveInt("vQuickStartConsonant", 0);
        SaveInt("vQuickEndConsonant", 0);
        SaveInt("vUseGrayIcon", 0);
        SaveInt("vShowOnStartUp", 1);
        SaveInt("vRunWithWindows", 1);
        SaveInt("vSupportMetroApp", 1);
        SaveInt("vRememberCode", 1);
        SaveInt("vOtherLanguage", 1);
        SaveInt("vTempOffOpenKey", 0);
        SaveInt("vFixChromiumBrowser", 0);
    }

    private static bool GetBool(string key, bool defaultValue)
    {
        return GetInt(key, defaultValue ? 1 : 0) != 0;
    }

    private static int GetInt(string key, int defaultValue)
    {
        using RegistryKey registryKey = OpenWritableKey();
        object? value = registryKey.GetValue(key);
        return value is int intValue ? intValue : defaultValue;
    }

    private static RegistryKey OpenWritableKey()
    {
        return Registry.CurrentUser.CreateSubKey(RegistryPath, writable: true)
            ?? throw new InvalidOperationException("Unable to open the OpenKey registry key.");
    }
}
