using System.Runtime.InteropServices;

namespace OpenKey.WinUI.Services;

/// <summary>
/// Shows the current OpenKey language state in the Windows notification area.
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private const int TrayIconId = 1001;
    private const uint NimAdd = 0x00000000;
    private const uint NimModify = 0x00000001;
    private const uint NimDelete = 0x00000002;
    private const uint NifIcon = 0x00000002;
    private const uint NifMessage = 0x00000001;
    private const uint NifTip = 0x00000004;
    private const uint ImageIcon = 1;
    private const uint LrLoadFromFile = 0x00000010;

    private readonly IntPtr _ownerHwnd;
    private readonly uint _callbackMessage;
    private IntPtr _currentIcon = IntPtr.Zero;
    private bool _isAdded;
    private int _language = -1;

    public TrayIconService(IntPtr ownerHwnd, uint callbackMessage)
    {
        _ownerHwnd = ownerHwnd;
        _callbackMessage = callbackMessage;
    }

    public void UpdateLanguage(int language)
    {
        language = language == 1 ? 1 : 0;
        if (_language == language)
        {
            return;
        }

        IntPtr nextIcon = LoadLanguageIcon(language);
        if (nextIcon == IntPtr.Zero)
        {
            return;
        }

        _language = language;
        IntPtr previousIcon = _currentIcon;
        _currentIcon = nextIcon;

        NotifyIconData data = CreateNotifyIconData(
            nextIcon,
            language == 1 ? "OpenKey - Tiếng Việt" : "OpenKey - English");

        ShellNotifyIcon(_isAdded ? NimModify : NimAdd, ref data);
        _isAdded = true;

        if (previousIcon != IntPtr.Zero)
        {
            DestroyIcon(previousIcon);
        }
    }

    public void Dispose()
    {
        if (_isAdded)
        {
            NotifyIconData data = CreateNotifyIconData(IntPtr.Zero, string.Empty);
            ShellNotifyIcon(NimDelete, ref data);
            _isAdded = false;
        }

        if (_currentIcon != IntPtr.Zero)
        {
            DestroyIcon(_currentIcon);
            _currentIcon = IntPtr.Zero;
        }
    }

    private NotifyIconData CreateNotifyIconData(IntPtr iconHandle, string tooltip)
    {
        return new NotifyIconData
        {
            Size = Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = _ownerHwnd,
            Id = TrayIconId,
            Flags = NifMessage | NifIcon | NifTip,
            CallbackMessage = _callbackMessage,
            IconHandle = iconHandle,
            Tip = tooltip
        };
    }

    private static IntPtr LoadLanguageIcon(int language)
    {
        string iconFileName = language == 1 ? "StatusViet.ico" : "StatusEng.ico";
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", iconFileName);
        return LoadImage(IntPtr.Zero, iconPath, ImageIcon, 16, 16, LrLoadFromFile);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW", SetLastError = true)]
    private static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadImageW", SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr instance, string name, uint type, int desiredWidth, int desiredHeight, uint load);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public int Size;
        public IntPtr WindowHandle;
        public int Id;
        public uint Flags;
        public uint CallbackMessage;
        public IntPtr IconHandle;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;

        public uint State;
        public uint StateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;

        public uint TimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;

        public uint InfoFlags;
        public Guid GuidItem;
        public IntPtr BalloonIconHandle;
    }
}
