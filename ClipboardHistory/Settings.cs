using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace ClipboardHistory;

public class AppSettings
{
    public int HotkeyVirtualKey { get; set; } = 0x91; // VK_SCROLL (Scroll Lock)
    public ModifierKeys HotkeyModifiers { get; set; } = ModifierKeys.None;

    public string GetHotkeyDisplayString()
    {
        var parts = new List<string>();

        if (HotkeyModifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (HotkeyModifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (HotkeyModifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (HotkeyModifiers.HasFlag(ModifierKeys.Windows))
            parts.Add("Win");

        var keyName = GetKeyName(HotkeyVirtualKey);
        parts.Add(keyName);

        return string.Join(" + ", parts);
    }

    public static string GetKeyName(int virtualKey)
    {
        return virtualKey switch
        {
            0x91 => "Scroll Lock",
            0x13 => "Pause",
            0x2C => "Print Screen",
            0x90 => "Num Lock",
            0x14 => "Caps Lock",
            0x70 => "F1",
            0x71 => "F2",
            0x72 => "F3",
            0x73 => "F4",
            0x74 => "F5",
            0x75 => "F6",
            0x76 => "F7",
            0x77 => "F8",
            0x78 => "F9",
            0x79 => "F10",
            0x7A => "F11",
            0x7B => "F12",
            0x6A => "Numpad *",
            0x6B => "Numpad +",
            0x6D => "Numpad -",
            0x6E => "Numpad .",
            0x6F => "Numpad /",
            0x60 => "Numpad 0",
            0x61 => "Numpad 1",
            0x62 => "Numpad 2",
            0x63 => "Numpad 3",
            0x64 => "Numpad 4",
            0x65 => "Numpad 5",
            0x66 => "Numpad 6",
            0x67 => "Numpad 7",
            0x68 => "Numpad 8",
            0x69 => "Numpad 9",
            0xC0 => "`",
            0xBD => "-",
            0xBB => "=",
            0xDB => "[",
            0xDD => "]",
            0xDC => "\\",
            0xBA => ";",
            0xDE => "'",
            0xBC => ",",
            0xBE => ".",
            0xBF => "/",
            >= 0x41 and <= 0x5A => ((char)virtualKey).ToString(), // A-Z
            >= 0x30 and <= 0x39 => ((char)virtualKey).ToString(), // 0-9
            _ => $"Key {virtualKey:X2}"
        };
    }
}

public class SettingsManager
{
    private readonly string _settingsFilePath;
    private AppSettings _settings;

    public SettingsManager()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ClipboardHistory");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _settings = LoadSettings();
    }

    public AppSettings Settings => _settings;

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not save settings: {ex.Message}");
        }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not load settings: {ex.Message}");
        }
        return new AppSettings();
    }
}
