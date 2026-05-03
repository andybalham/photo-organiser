namespace PhotoOrganiser.Helpers;

public static class FileNameHelper
{
    private static readonly char[] InvalidFolderChars =
        Path.GetInvalidFileNameChars().Except(['/', '\\']).ToArray();

    public static string SanitiseFolderName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var chars = name.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (Array.IndexOf(InvalidFolderChars, chars[i]) >= 0)
                chars[i] = '-';
        return new string(chars).Trim();
    }

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
