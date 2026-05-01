namespace PhotoOrganiser.Helpers;

public static class FileTypes
{
    public static readonly IReadOnlySet<string> Images = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp",
        ".heic", ".heif", ".raw", ".cr2", ".cr3", ".nef", ".arw", ".orf",
        ".rw2", ".dng", ".pef", ".srw", ".raf"
    };

    public static readonly IReadOnlySet<string> Videos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".m4v", ".3gp", ".flv",
        ".mpg", ".mpeg", ".mts", ".m2ts"
    };

    public static bool IsSupported(string extension) =>
        Images.Contains(extension) || Videos.Contains(extension);
}
