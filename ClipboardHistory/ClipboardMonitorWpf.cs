using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;

namespace ClipboardHistory;

public class ClipboardMonitorWpf
{
    private readonly ClipboardHistoryManager _historyManager;
    private readonly DispatcherTimer _timer;
    private string _lastClipboardHash = string.Empty;

    public ClipboardMonitorWpf(ClipboardHistoryManager historyManager)
    {
        _historyManager = historyManager;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += CheckClipboard;
    }

    public event EventHandler<ClipboardEntry>? ClipboardChanged;

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void CheckClipboard(object? sender, EventArgs e)
    {
        try
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    var hash = GetImageHash(image);
                    if (hash != _lastClipboardHash)
                    {
                        _lastClipboardHash = hash;
                        var imagePath = SaveImage(image);
                        if (imagePath != null)
                        {
                            var entry = _historyManager.AddImageEntry(imagePath);
                            ClipboardChanged?.Invoke(this, entry);
                        }
                    }
                }
            }
            else if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    var hash = $"text:{text.GetHashCode()}";
                    if (hash != _lastClipboardHash)
                    {
                        _lastClipboardHash = hash;
                        _historyManager.AddEntry(text, ClipboardContentType.Text);

                        var entry = _historyManager.History.FirstOrDefault();
                        if (entry != null)
                        {
                            ClipboardChanged?.Invoke(this, entry);
                        }
                    }
                }
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                var filePaths = string.Join(Environment.NewLine, files.Cast<string>());

                if (!string.IsNullOrEmpty(filePaths))
                {
                    var hash = $"files:{filePaths.GetHashCode()}";
                    if (hash != _lastClipboardHash)
                    {
                        _lastClipboardHash = hash;
                        _historyManager.AddEntry(filePaths, ClipboardContentType.FilePaths);

                        var entry = _historyManager.History.FirstOrDefault();
                        if (entry != null)
                        {
                            ClipboardChanged?.Invoke(this, entry);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore clipboard access errors
        }
    }

    private string GetImageHash(BitmapSource image)
    {
        try
        {
            // Convert image to bytes and compute hash
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            var bytes = stream.ToArray();

            // Use MD5 for speed (not security-critical here)
            var hashBytes = MD5.HashData(bytes);
            return $"img:{Convert.ToHexString(hashBytes)}";
        }
        catch
        {
            // Fallback to dimensions only
            return $"img:{image.PixelWidth}x{image.PixelHeight}";
        }
    }

    private string? SaveImage(BitmapSource image)
    {
        try
        {
            var fileName = $"clip_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            var filePath = Path.Combine(_historyManager.ImagesFolder, fileName);

            using var fileStream = new FileStream(filePath, FileMode.Create);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            encoder.Save(fileStream);

            return filePath;
        }
        catch
        {
            return null;
        }
    }
}
