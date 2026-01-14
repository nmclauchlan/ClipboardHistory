using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using Forms = System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;

namespace ClipboardHistory;

public partial class MainWindow : Window
{
    private readonly ClipboardHistoryManager _historyManager;
    private readonly ClipboardMonitorWpf _clipboardMonitor;
    private readonly ObservableCollection<ClipboardItemViewModel> _items;
    private bool _isVisible;
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();

        _historyManager = new ClipboardHistoryManager(maxEntries: 100);
        _clipboardMonitor = new ClipboardMonitorWpf(_historyManager);
        _items = new ObservableCollection<ClipboardItemViewModel>();

        ClipboardList.ItemsSource = _items;

        _clipboardMonitor.ClipboardChanged += (s, e) =>
        {
            Dispatcher.Invoke(RefreshList);
        };

        _clipboardMonitor.Start();

        PositionWindow();
        RefreshList();
    }

    private void PositionWindow()
    {
        var screen = Forms.Screen.PrimaryScreen?.WorkingArea ?? new System.Drawing.Rectangle(0, 0, 1920, 1080);
        Width = 380;
        Height = screen.Height;
        Left = screen.Right - Width;
        Top = screen.Top;
    }

    public void ShowPanel()
    {
        if (_isVisible) return;

        PositionWindow();
        RefreshList();
        Show();
        Activate();
        SearchBox.Focus();

        var slideIn = (Storyboard)FindResource("SlideIn");
        var border = (System.Windows.Controls.Border)Content;
        slideIn.Begin(border);

        _isVisible = true;
    }

    public void HidePanel()
    {
        if (!_isVisible) return;

        var slideOut = (Storyboard)FindResource("SlideOut");
        var border = (System.Windows.Controls.Border)Content;

        slideOut.Completed += (s, e) =>
        {
            Hide();
            SearchBox.Clear();
        };

        slideOut.Begin(border);
        _isVisible = false;
    }

    public void TogglePanel()
    {
        if (_isVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public void ForceClose()
    {
        _forceClose = true;
        _clipboardMonitor.Stop();
        Close();
    }

    private void RefreshList()
    {
        var searchText = SearchBox.Text.Trim();
        _items.Clear();

        var entries = _historyManager.History
            .Where(e => string.IsNullOrEmpty(searchText) ||
                       e.Content.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                       (e.ContentType == ClipboardContentType.Image && "image screenshot".Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            .Take(50);

        foreach (var entry in entries)
        {
            _items.Add(new ClipboardItemViewModel(entry));
        }
    }

    private void CopySelectedItem()
    {
        if (ClipboardList.SelectedItem is ClipboardItemViewModel item)
        {
            item.CopyToClipboard();
            HidePanel();
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (_isVisible)
        {
            HidePanel();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_forceClose)
        {
            e.Cancel = true;
            HidePanel();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            HidePanel();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        RefreshList();
    }

    private void ClipboardList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopySelectedItem();
    }

    private void ClipboardList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CopySelectedItem();
            e.Handled = true;
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Clear all clipboard history?",
            "Confirm Clear",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            _historyManager.ClearHistory();
            RefreshList();
        }
    }
}

public class ClipboardItemViewModel
{
    private readonly ClipboardEntry _entry;
    private ImageSource? _thumbnailSource;

    public ClipboardItemViewModel(ClipboardEntry entry)
    {
        _entry = entry;
    }

    public int Id => _entry.Id;
    public string Content => _entry.Content;
    public string? ImagePath => _entry.ImagePath;
    public ClipboardContentType ContentType => _entry.ContentType;
    public string Preview => _entry.GetPreview(60);

    public Visibility ImageVisibility =>
        _entry.ContentType == ClipboardContentType.Image ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IconVisibility =>
        _entry.ContentType != ClipboardContentType.Image ? Visibility.Visible : Visibility.Collapsed;

    public string TypeIcon => _entry.ContentType switch
    {
        ClipboardContentType.Text => "\uE8C1",      // Document icon
        ClipboardContentType.FilePaths => "\uE8B7", // Folder icon
        _ => "\uE8C1"
    };

    public ImageSource? ThumbnailSource
    {
        get
        {
            if (_thumbnailSource == null && _entry.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(_entry.ImagePath))
            {
                try
                {
                    if (File.Exists(_entry.ImagePath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(_entry.ImagePath);
                        bitmap.DecodePixelWidth = 120; // Thumbnail size
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                        _thumbnailSource = bitmap;
                    }
                }
                catch
                {
                    // Ignore thumbnail loading errors
                }
            }
            return _thumbnailSource;
        }
    }

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.Now - _entry.Timestamp;

            if (diff.TotalSeconds < 60)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";

            return _entry.Timestamp.ToString("MMM d");
        }
    }

    public void CopyToClipboard()
    {
        try
        {
            if (_entry.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(_entry.ImagePath))
            {
                if (File.Exists(_entry.ImagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_entry.ImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    Clipboard.SetImage(bitmap);
                }
            }
            else if (_entry.ContentType == ClipboardContentType.FilePaths)
            {
                var files = new System.Collections.Specialized.StringCollection();
                files.AddRange(_entry.Content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
                Clipboard.SetFileDropList(files);
            }
            else
            {
                Clipboard.SetText(_entry.Content);
            }
        }
        catch
        {
            // Fallback to text if something fails
            if (!string.IsNullOrEmpty(_entry.Content))
            {
                Clipboard.SetText(_entry.Content);
            }
        }
    }
}
