using System.Text.Json;
using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public class ShareableDatesService : IShareableDatesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private record SharedDatesFile(
        IReadOnlyList<SpecialDate> SpecialDates,
        IReadOnlyList<DateRange> DateRanges);

    public string? LinkedFilePath { get; protected set; }

    protected virtual string GetSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhotoOrganiser", "settings.json");

    public ShareableDatesService()
    {
        LinkedFilePath = LoadLinkedPath();
    }

    protected ShareableDatesService(bool _)
    {
        // deferred init — subclass calls Initialize() after setting its fields
    }

    protected void Initialize()
    {
        LinkedFilePath = LoadLinkedPath();
    }

    private string? LoadLinkedPath()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path)) return null;
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.TryGetProperty("linkedDatesFile", out var el))
                return string.IsNullOrEmpty(el.GetString()) ? null : el.GetString();
        }
        catch { }
        return null;
    }

    private void SaveLinkedPath(string? filePath)
    {
        try
        {
            var settingsPath = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

            Dictionary<string, object?> obj = new() { ["linkedDatesFile"] = filePath };

            if (File.Exists(settingsPath))
            {
                try
                {
                    var existing = JsonDocument.Parse(File.ReadAllText(settingsPath));
                    foreach (var prop in existing.RootElement.EnumerateObject())
                        if (prop.Name != "linkedDatesFile")
                            obj[prop.Name] = prop.Value.Clone();
                }
                catch { }
            }

            File.WriteAllText(settingsPath, JsonSerializer.Serialize(obj, JsonOptions));
        }
        catch { }
    }

    public void LinkFile(string path, IEnumerable<SpecialDate> currentDates, IEnumerable<DateRange> currentRanges)
    {
        if (File.Exists(path))
        {
            // existing file — it will be loaded by caller, just record path
        }
        else
        {
            // new file — save current data into it
            WriteFile(path, currentDates, currentRanges);
        }

        LinkedFilePath = path;
        SaveLinkedPath(path);
    }

    public void SaveToFile(string path, IEnumerable<SpecialDate> dates, IEnumerable<DateRange> ranges)
    {
        WriteFile(path, dates, ranges);
    }

    public void Unlink()
    {
        LinkedFilePath = null;
        SaveLinkedPath(null);
    }

    public (IReadOnlyList<SpecialDate> SpecialDates, IReadOnlyList<DateRange> DateRanges)? TryAutoLoad()
    {
        if (LinkedFilePath == null) return null;

        if (!File.Exists(LinkedFilePath))
        {
            // file gone — clear path silently
            LinkedFilePath = null;
            SaveLinkedPath(null);
            return null;
        }

        return ReadFile(LinkedFilePath);
    }

    public void AutoSave(IEnumerable<SpecialDate> dates, IEnumerable<DateRange> ranges, Action<string> onError)
    {
        if (LinkedFilePath == null) return;
        try
        {
            WriteFile(LinkedFilePath, dates, ranges);
        }
        catch (Exception ex)
        {
            onError(ex.Message);
        }
    }

    private static void WriteFile(string path, IEnumerable<SpecialDate> dates, IEnumerable<DateRange> ranges)
    {
        var data = new SharedDatesFile([.. dates], [.. ranges]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
    }

    private static (IReadOnlyList<SpecialDate>, IReadOnlyList<DateRange>)? ReadFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SharedDatesFile>(json, JsonOptions);
            if (data == null) return null;
            return (data.SpecialDates ?? [], data.DateRanges ?? []);
        }
        catch { return null; }
    }
}
