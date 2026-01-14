using System.Windows;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace ClipboardHistory;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private GlobalKeyboardHook? _keyboardHook;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Create main window (hidden initially)
        _mainWindow = new MainWindow();

        // Setup system tray icon
        SetupNotifyIcon();

        // Setup global keyboard hook
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.ScrollLockPressed += OnScrollLockPressed;
        _keyboardHook.Start();
    }

    private void SetupNotifyIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Visible = true,
            Text = "Clipboard History (Scroll Lock to open)"
        };

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => _mainWindow?.ShowPanel());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => _mainWindow?.ShowPanel();
    }

    private void OnScrollLockPressed(object? sender, EventArgs e)
    {
        Current.Dispatcher.Invoke(() =>
        {
            _mainWindow?.TogglePanel();
        });
    }

    private void ExitApplication()
    {
        _keyboardHook?.Stop();
        _notifyIcon?.Dispose();
        _mainWindow?.ForceClose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _keyboardHook?.Stop();
        _notifyIcon?.Dispose();
        base.OnExit(e);
    }
}
