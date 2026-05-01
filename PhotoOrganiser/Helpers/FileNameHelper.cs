namespace PhotoOrganiser.Helpers;

public static class FileNameHelper
{
    /// <summary>
    /// Returns a destination path that doesn't collide with any existing file.
    /// Appends _1, _2, … before the extension until a free name is found.
    /// The <paramref name="exists"/> delegate is injected so the logic is unit-testable.
    /// </summary>
    public static string GetUniqueDestinationPath(string destPath, Func<string, bool> exists)
    {
        if (!exists(destPath))
            return destPath;

        var dir  = Path.GetDirectoryName(destPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(destPath);
        var ext  = Path.GetExtension(destPath);

        for (int n = 1; ; n++)
        {
            var candidate = Path.Combine(dir, $"{name}_{n}{ext}");
            if (!exists(candidate))
                return candidate;
        }
    }
}
