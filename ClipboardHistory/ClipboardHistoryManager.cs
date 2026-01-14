using System.IO;
using System.Text.Json;

namespace ClipboardHistory;

public class ClipboardHistoryManager
{
    private readonly List<ClipboardEntry> _history = [];
    private readonly string _historyFilePath;
    private readonly string _imagesFolder;
    private readonly int _maxEntries;
    private int _nextId = 1;

    public ClipboardHistoryManager(int maxEntries = 100)
    {
        _maxEntries = maxEntries;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ClipboardHistory");
        _imagesFolder = Path.Combine(appFolder, "images");
        Directory.CreateDirectory(appFolder);
        Directory.CreateDirectory(_imagesFolder);
        _historyFilePath = Path.Combine(appFolder, "history.json");
        LoadHistory();
    }

    public string ImagesFolder => _imagesFolder;
    public IReadOnlyList<ClipboardEntry> History => _history.AsReadOnly();

    public void AddEntry(string content, ClipboardContentType contentType)
    {
        if (string.IsNullOrEmpty(content))
            return;

        // Don't add duplicate of the most recent entry
        if (_history.Count > 0 && _history[0].Content == content && _history[0].ContentType == contentType)
            return;

        var entry = new ClipboardEntry
        {
            Id = _nextId++,
            Content = content,
            Timestamp = DateTime.Now,
            ContentType = contentType
        };

        _history.Insert(0, entry);
        TrimHistory();
        SaveHistory();
    }

    public ClipboardEntry AddImageEntry(string imagePath)
    {
        var entry = new ClipboardEntry
        {
            Id = _nextId++,
            Content = string.Empty,
            ImagePath = imagePath,
            Timestamp = DateTime.Now,
            ContentType = ClipboardContentType.Image
        };

        _history.Insert(0, entry);
        TrimHistory();
        SaveHistory();
        return entry;
    }

    private void TrimHistory()
    {
        while (_history.Count > _maxEntries)
        {
            var oldEntry = _history[_history.Count - 1];
            // Delete old image files
            if (oldEntry.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(oldEntry.ImagePath))
            {
                try { File.Delete(oldEntry.ImagePath); } catch { }
            }
            _history.RemoveAt(_history.Count - 1);
        }
    }

    public ClipboardEntry? GetEntry(int id)
    {
        return _history.FirstOrDefault(e => e.Id == id);
    }

    public void ClearHistory()
    {
        // Delete all image files
        foreach (var entry in _history.Where(e => e.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(e.ImagePath)))
        {
            try { File.Delete(entry.ImagePath!); } catch { }
        }
        _history.Clear();
        _nextId = 1;
        SaveHistory();
    }

    public void DeleteEntry(int id)
    {
        var entry = _history.FirstOrDefault(e => e.Id == id);
        if (entry != null)
        {
            if (entry.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(entry.ImagePath))
            {
                try { File.Delete(entry.ImagePath); } catch { }
            }
            _history.Remove(entry);
            SaveHistory();
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                var entries = JsonSerializer.Deserialize<List<ClipboardEntry>>(json);
                if (entries != null)
                {
                    // Filter out entries with missing image files
                    foreach (var entry in entries)
                    {
                        if (entry.ContentType == ClipboardContentType.Image && !string.IsNullOrEmpty(entry.ImagePath))
                        {
                            if (!File.Exists(entry.ImagePath))
                                continue;
                        }
                        _history.Add(entry);
                    }
                    _nextId = _history.Count > 0 ? _history.Max(e => e.Id) + 1 : 1;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not load history: {ex.Message}");
        }
    }

    private void SaveHistory()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_history, options);
            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Could not save history: {ex.Message}");
        }
    }
}
