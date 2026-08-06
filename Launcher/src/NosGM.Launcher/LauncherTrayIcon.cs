// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace NosGM.Launcher;

internal sealed class LauncherTrayIcon : IDisposable
{
    private const int WmApp = 0x8000;
    private const int CallbackMessage = WmApp + 0x51;
    private const int WmLeftButtonUp = 0x0202;
    private const int WmLeftButtonDoubleClick = 0x0203;
    private const int WmRightButtonUp = 0x0205;

    private const uint NotifyAdd = 0x00000000;
    private const uint NotifyModify = 0x00000001;
    private const uint NotifyDelete = 0x00000002;
    private const uint FlagMessage = 0x00000001;
    private const uint FlagIcon = 0x00000002;
    private const uint FlagTip = 0x00000004;
    private const uint FlagInfo = 0x00000010;
    private const uint InfoNone = 0x00000000;
    private const uint InfoInfo = 0x00000001;
    private const uint InfoWarning = 0x00000002;
    private const int ApplicationIconId = 32512;

    private readonly Window _owner;
    private readonly HwndSource _source;
    private readonly IntPtr _windowHandle;
    private readonly IntPtr _iconHandle;
    private readonly uint _iconId;
    private bool _visible;
    private bool _disposed;
    private bool _spanish;

    public LauncherTrayIcon(Window owner, bool spanish)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _spanish = spanish;
        _windowHandle = new WindowInteropHelper(owner).Handle;
        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("The launcher window handle is not available.");
        }

        _source = HwndSource.FromHwnd(_windowHandle)
                  ?? throw new InvalidOperationException("The launcher message source is unavailable.");
        _source.AddHook(WindowMessageHook);
        _iconHandle = LoadIconNative(IntPtr.Zero, new IntPtr(ApplicationIconId));
        if (_iconHandle == IntPtr.Zero)
        {
            _source.RemoveHook(WindowMessageHook);
            throw new InvalidOperationException("Windows could not load the companion tray icon.");
        }

        _iconId = 1;
    }

    public event EventHandler? OpenRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public void SetLanguage(bool spanish)
    {
        ThrowIfDisposed();
        _spanish = spanish;
        if (_visible)
        {
            _ = ShellNotifyIcon(NotifyModify, CreateData(FlagMessage | FlagIcon | FlagTip));
        }
    }

    public void SetVisible(bool visible)
    {
        ThrowIfDisposed();
        if (_visible == visible)
        {
            return;
        }

        if (visible)
        {
            if (!ShellNotifyIcon(NotifyAdd, CreateData(FlagMessage | FlagIcon | FlagTip)))
            {
                throw new InvalidOperationException("Windows could not create the NosGM companion icon.");
            }

            _visible = true;
        }
        else
        {
            _ = ShellNotifyIcon(NotifyDelete, CreateData(0));
            _visible = false;
        }
    }

    public void ShowNotification(string title, string message, bool warning = false)
    {
        ThrowIfDisposed();
        if (!_visible)
        {
            SetVisible(true);
        }

        var data = CreateData(FlagMessage | FlagIcon | FlagTip | FlagInfo);
        data.InfoTitle = Limit(title, 63);
        data.Info = Limit(message, 255);
        data.InfoFlags = warning ? InfoWarning : InfoInfo;
        _ = ShellNotifyIcon(NotifyModify, data);
    }

    private IntPtr WindowMessageHook(
        IntPtr hwnd,
        int message,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handled)
    {
        if (message != CallbackMessage)
        {
            return IntPtr.Zero;
        }

        handled = true;
        switch (unchecked((int)lParam.ToInt64()))
        {
            case WmLeftButtonUp:
            case WmLeftButtonDoubleClick:
                OpenRequested?.Invoke(this, EventArgs.Empty);
                break;
            case WmRightButtonUp:
                _owner.Dispatcher.BeginInvoke(ShowContextMenu);
                break;
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        if (_disposed)
        {
            return;
        }

        var menu = new ContextMenu
        {
            Placement = PlacementMode.MousePoint,
            StaysOpen = false
        };
        var open = new MenuItem
        {
            Header = _spanish ? "Abrir NosGM" : "Open NosGM",
            FontWeight = FontWeights.SemiBold
        };
        open.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        var settings = new MenuItem
        {
            Header = _spanish ? "Alertas y Companion" : "Alerts and Companion"
        };
        settings.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        var exit = new MenuItem
        {
            Header = _spanish ? "Salir completamente" : "Exit completely"
        };
        exit.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(open);
        menu.Items.Add(settings);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        menu.IsOpen = true;
    }

    private NotifyIconData CreateData(uint flags)
        => new()
        {
            Size = (uint)Marshal.SizeOf<NotifyIconData>(),
            WindowHandle = _windowHandle,
            IconId = _iconId,
            Flags = flags,
            CallbackMessage = CallbackMessage,
            IconHandle = _iconHandle,
            Tip = Limit(
                _spanish
                    ? "NosGM Companion • doble clic para abrir"
                    : "NosGM Companion • double-click to open",
                127),
            State = 0,
            StateMask = 0,
            Info = string.Empty,
            TimeoutOrVersion = 0,
            InfoTitle = string.Empty,
            InfoFlags = InfoNone,
            ItemGuid = Guid.Empty,
            BalloonIconHandle = IntPtr.Zero
        };

    private static string Limit(string? value, int maximumLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_visible)
        {
            _ = ShellNotifyIcon(NotifyDelete, CreateData(0));
            _visible = false;
        }

        _source.RemoveHook(WindowMessageHook);
        _disposed = true;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public IntPtr WindowHandle;
        public uint IconId;
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
        public Guid ItemGuid;
        public IntPtr BalloonIconHandle;
    }

    [DllImport(
        "shell32.dll",
        EntryPoint = "Shell_NotifyIconW",
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIconNative(
        uint message,
        ref NotifyIconData data);

    private static bool ShellNotifyIcon(uint message, NotifyIconData data)
        => ShellNotifyIconNative(message, ref data);

    [DllImport(
        "user32.dll",
        EntryPoint = "LoadIconW",
        CharSet = CharSet.Unicode,
        ExactSpelling = true,
        SetLastError = true)]
    private static extern IntPtr LoadIconNative(
        IntPtr instance,
        IntPtr iconName);
}
