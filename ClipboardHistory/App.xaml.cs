using System.Windows;
using System.Drawing;
using System.Reflection;
using Forms = System.Windows.Forms;

namespace ClipboardHistory;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private GlobalKeyboardHook? _keyboardHook;
    private SettingsManager? _settingsManager;
    private SettingsWindow? _settingsWindow;

    public SettingsManager? SettingsManager => _settingsManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize settings manager
        _settingsManager = new SettingsManager();

        // Create main window (hidden initially)
        _mainWindow = new MainWindow(_settingsManager);

        // Setup system tray icon
        SetupNotifyIcon();

        // Setup global keyboard hook with settings
        _keyboardHook = new GlobalKeyboardHook
        {
            TargetVirtualKey = _settingsManager.Settings.HotkeyVirtualKey,
            RequiredModifiers = _settingsManager.Settings.HotkeyModifiers
        };
        _keyboardHook.HotkeyPressed += OnHotkeyPressed;
        _keyboardHook.Start();
    }

    private void SetupNotifyIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Visible = true,
            Text = $"Clipboard History ({_settingsManager!.Settings.GetHotkeyDisplayString()} to open)"
        };

        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add("Show", null, (s, e) => _mainWindow?.ShowPanel());
        contextMenu.Items.Add("Settings", null, (s, e) => ShowSettings());
        contextMenu.Items.Add("Clear History", null, (s, e) => ClearHistory());
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => _mainWindow?.ShowPanel();
    }

    private static Icon LoadAppIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "ClipboardHistory.app.ico";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            return new Icon(stream);
        }
        return SystemIcons.Application;
    }

    private void ShowSettings()
    {
        if (_settingsWindow != null && _settingsWindow.IsVisible)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settingsManager!);
        _settingsWindow.HotkeyChanged += OnHotkeyChanged;
        _settingsWindow.ShowDialog();
    }

    private void OnHotkeyChanged(object? sender, HotkeyChangedEventArgs e)
    {
        _keyboardHook?.UpdateHotkey(e.VirtualKey, e.Modifiers);
        _notifyIcon!.Text = $"Clipboard History ({_settingsManager!.Settings.GetHotkeyDisplayString()} to open)";
        _mainWindow?.UpdateFooterText();
    }

    private void ClearHistory()
    {
        var result = System.Windows.MessageBox.Show(
            "Clear all clipboard history?",
            "Confirm Clear",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _mainWindow?.ClearHistory();
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
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
