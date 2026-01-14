using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace ClipboardHistoryInstaller;

public partial class MainWindow : Window
{
    private const string AppName = "ClipboardHistory";
    private const string ExeName = "ClipboardHistory.exe";
    private readonly string _installPath;
    private readonly string _exePath;
    private bool _isUpdate;

    public MainWindow()
    {
        InitializeComponent();

        // Install to Program Files
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        _installPath = Path.Combine(programFiles, AppName);
        _exePath = Path.Combine(_installPath, ExeName);

        // Check if this is an update
        _isUpdate = File.Exists(_exePath);
        if (_isUpdate)
        {
            InstallButton.Content = "Update";
            Title = "Clipboard History Updater";
        }
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        // Show progress
        WelcomePage.Visibility = Visibility.Collapsed;
        ProgressPage.Visibility = Visibility.Visible;
        InstallButton.IsEnabled = false;
        CancelButton.IsEnabled = false;

        try
        {
            await Task.Run(() => PerformInstallation());

            // Show complete page
            ProgressPage.Visibility = Visibility.Collapsed;
            CompletePage.Visibility = Visibility.Visible;

            if (_isUpdate)
            {
                CompleteTitle.Text = "Update Complete!";
                CompleteMessage.Text = "Clipboard History has been updated successfully.";
            }

            InstallButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;

            // Launch if requested
            if (LaunchAfterInstallCheckbox.IsChecked == true)
            {
                LaunchApplication();
            }
        }
        catch (Exception ex)
        {
            // Show error page
            ProgressPage.Visibility = Visibility.Collapsed;
            ErrorPage.Visibility = Visibility.Visible;
            ErrorMessage.Text = ex.Message;

            InstallButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;
        }
    }

    private void PerformInstallation()
    {
        // Step 1: Close running instance
        UpdateStatus("Checking for running instances...");
        CloseRunningInstance();

        // Step 2: Create install directory
        UpdateStatus("Creating installation directory...");
        Directory.CreateDirectory(_installPath);

        // Step 3: Extract embedded executable
        UpdateStatus("Extracting application files...");
        ExtractEmbeddedExe();

        // Step 4: Create Start Menu shortcut
        UpdateStatus("Creating shortcuts...");
        CreateStartMenuShortcut();

        // Step 5: Set startup if requested
        Dispatcher.Invoke(() =>
        {
            if (StartWithWindowsCheckbox.IsChecked == true)
            {
                UpdateStatus("Configuring startup...");
                SetStartupRegistry(true);
            }
        });

        // Step 6: Register uninstaller
        UpdateStatus("Registering application...");
        RegisterUninstaller();

        UpdateStatus("Installation complete!");
        Thread.Sleep(500);
    }

    private void UpdateStatus(string message)
    {
        Dispatcher.Invoke(() =>
        {
            DetailText.Text = message;
        });
    }

    private void CloseRunningInstance()
    {
        try
        {
            var processes = Process.GetProcessesByName("ClipboardHistory");
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit(5000);
            }
        }
        catch
        {
            // Ignore errors closing process
        }
    }

    private void ExtractEmbeddedExe()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("ClipboardHistory.exe"));

        if (resourceName == null)
            throw new Exception("Could not find embedded application. Please re-download the installer.");

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception("Could not read embedded application.");

        using var fileStream = new FileStream(_exePath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fileStream);
    }

    private void CreateStartMenuShortcut()
    {
        try
        {
            var startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            var shortcutPath = Path.Combine(startMenuPath, "Clipboard History.lnk");

            // Use PowerShell to create shortcut (works without COM interop)
            var script = $@"
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
$Shortcut.TargetPath = '{_exePath}'
$Shortcut.Description = 'Clipboard History Manager'
$Shortcut.Save()
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit(10000);
        }
        catch
        {
            // Shortcut creation is optional
        }
    }

    private void SetStartupRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key != null)
            {
                if (enable)
                {
                    key.SetValue(AppName, $"\"{_exePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
        catch
        {
            // Startup registration is optional
        }
    }

    private void RegisterUninstaller()
    {
        try
        {
            var uninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + AppName;
            using var key = Registry.LocalMachine.CreateSubKey(uninstallKey);

            if (key != null)
            {
                key.SetValue("DisplayName", "Clipboard History");
                key.SetValue("DisplayVersion", "1.0.0");
                key.SetValue("Publisher", "Clipboard History");
                key.SetValue("InstallLocation", _installPath);
                key.SetValue("DisplayIcon", _exePath);
                key.SetValue("UninstallString", $"\"{_exePath}\" --uninstall");
                key.SetValue("NoModify", 1);
                key.SetValue("NoRepair", 1);
            }
        }
        catch
        {
            // Uninstaller registration is optional
        }
    }

    private void LaunchApplication()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _exePath,
                UseShellExecute = true
            });
        }
        catch
        {
            // Launch is optional
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
