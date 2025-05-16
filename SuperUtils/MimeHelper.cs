public static class MimeHelper
{
    public static String TXT = ".txt";
    public static String HTML = ".html";
    public static String HTM = ".htm";
    public static String JPG = ".jpg";
    public static String JPEG = ".jpeg";
    public static String PNG = ".png";
    public static String GIF = ".gif";
    public static String BMP = ".bmp";
    public static String PDF = ".pdf";
    public static String DOC = ".doc";
    public static String DOCX = ".docx";
    public static String XLS = ".xls";
    public static String XLSX = ".xlsx";
    public static String ZIP = ".zip";
    public static String RAR = ".rar";
    public static String MP3 = ".mp3";
    public static String MP4 = ".mp4";

    private static readonly Dictionary<string, string> _mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".txt", "text/plain" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".jpg", "image/jpeg" }, 
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".mp3", "audio/mpeg" },
        { ".mp4", "video/mp4" },
        // Add more as needed
    };

    public static string GetMimeType(string filePath)
    {
        string ext = Path.GetExtension(filePath);
        if (ext != null && _mimeTypes.TryGetValue(ext, out string mime))
        {
            return mime;
        }

        return "application/octet-stream"; // Default binary stream
    }

    public static string GetExtension(string mimeType)
    {
        return _mimeTypes.FirstOrDefault(pair => pair.Value == mimeType).Key ?? "";
    }
}
