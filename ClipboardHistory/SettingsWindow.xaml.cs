using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace ClipboardHistory;

public partial class SettingsWindow : Window
{
    private readonly SettingsManager _settingsManager;
    private int _pendingVirtualKey;
    private ModifierKeys _pendingModifiers;
    private bool _isCapturing;

    public event EventHandler<HotkeyChangedEventArgs>? HotkeyChanged;

    public SettingsWindow(SettingsManager settingsManager)
    {
        InitializeComponent();
        _settingsManager = settingsManager;

        // Set window icon from embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("ClipboardHistory.app.ico");
        if (stream != null)
        {
            Icon = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }

        // Load current settings
        _pendingVirtualKey = _settingsManager.Settings.HotkeyVirtualKey;
        _pendingModifiers = _settingsManager.Settings.HotkeyModifiers;

        UpdateHotkeyDisplay();
    }

    private void UpdateHotkeyDisplay()
    {
        var tempSettings = new AppSettings
        {
            HotkeyVirtualKey = _pendingVirtualKey,
            HotkeyModifiers = _pendingModifiers
        };
        HotkeyInput.Text = tempSettings.GetHotkeyDisplayString();
    }

    private void HotkeyInput_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isCapturing)
            return;

        e.Handled = true;

        // Ignore modifier-only keys when pressed alone
        if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
            e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
            e.Key == Key.LeftShift || e.Key == Key.RightShift ||
            e.Key == Key.LWin || e.Key == Key.RWin ||
            e.Key == Key.System)
        {
            return;
        }

        // Convert to virtual key code
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        _pendingVirtualKey = KeyInterop.VirtualKeyFromKey(key);

        // Capture modifiers from keyboard state
        _pendingModifiers = ModifierKeys.None;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            _pendingModifiers |= ModifierKeys.Control;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
            _pendingModifiers |= ModifierKeys.Alt;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            _pendingModifiers |= ModifierKeys.Shift;
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
            _pendingModifiers |= ModifierKeys.Windows;

        UpdateHotkeyDisplay();

        _isCapturing = false;
        HotkeyInput.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3d3d3d"));
    }

    private void HotkeyInput_GotFocus(object sender, RoutedEventArgs e)
    {
        _isCapturing = true;
        HotkeyInput.Text = "Press a key combination...";
        HotkeyInput.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4d4d4d"));
    }

    private void HotkeyInput_LostFocus(object sender, RoutedEventArgs e)
    {
        _isCapturing = false;
        UpdateHotkeyDisplay();
        HotkeyInput.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3d3d3d"));
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _pendingVirtualKey = 0x91; // VK_SCROLL
        _pendingModifiers = ModifierKeys.None;
        UpdateHotkeyDisplay();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsManager.Settings.HotkeyVirtualKey = _pendingVirtualKey;
        _settingsManager.Settings.HotkeyModifiers = _pendingModifiers;
        _settingsManager.Save();

        HotkeyChanged?.Invoke(this, new HotkeyChangedEventArgs(_pendingVirtualKey, _pendingModifiers));

        Close();
    }
}

public class HotkeyChangedEventArgs : EventArgs
{
    public int VirtualKey { get; }
    public ModifierKeys Modifiers { get; }

    public HotkeyChangedEventArgs(int virtualKey, ModifierKeys modifiers)
    {
        VirtualKey = virtualKey;
        Modifiers = modifiers;
    }
}
