namespace ClipboardHistory;

public class ClipboardEntry
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public DateTime Timestamp { get; set; }
    public ClipboardContentType ContentType { get; set; }

    public string GetPreview(int maxLength = 50)
    {
        if (ContentType == ClipboardContentType.Image)
            return "[Screenshot/Image]";

        if (string.IsNullOrEmpty(Content))
            return "[Empty]";

        var singleLine = Content.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        if (singleLine.Length <= maxLength)
            return singleLine;

        return singleLine[..(maxLength - 3)] + "...";
    }
}

public enum ClipboardContentType
{
    Text,
    FilePaths,
    Image,
    Unknown
}
