using System.Text.Json;
using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public class SpecialDateService : ISpecialDateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private List<SpecialDate>? _cache;
    private List<DateRange>? _rangeCache;

    protected virtual string GetFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhotoOrganiser", "special_dates.json");

    protected virtual string GetRangeFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PhotoOrganiser", "date_ranges.json");

    public IReadOnlyList<SpecialDate> GetAll()
    {
        if (_cache != null) return _cache;
        var path = GetFilePath();
        if (!File.Exists(path)) return _cache = [];
        try
        {
            var json = File.ReadAllText(path);
            _cache = JsonSerializer.Deserialize<List<SpecialDate>>(json, JsonOptions) ?? [];
        }
        catch { _cache = []; }
        return _cache;
    }

    public void Save(IEnumerable<SpecialDate> dates)
    {
        _cache = [.. dates];
        var path = GetFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(_cache, JsonOptions));
    }

    public SpecialDate? Match(DateTime date)
    {
        foreach (var sd in GetAll())
        {
            if (sd.Month != date.Month || sd.Day != date.Day) continue;
            if (sd.Year == null || sd.Year == date.Year)
                return sd;
        }
        return null;
    }

    public IReadOnlyList<DateRange> GetAllRanges()
    {
        if (_rangeCache != null) return _rangeCache;
        var path = GetRangeFilePath();
        if (!File.Exists(path)) return _rangeCache = [];
        try
        {
            var json = File.ReadAllText(path);
            _rangeCache = JsonSerializer.Deserialize<List<DateRange>>(json, JsonOptions) ?? [];
        }
        catch { _rangeCache = []; }
        return _rangeCache;
    }

    public void SaveRanges(IEnumerable<DateRange> ranges)
    {
        _rangeCache = [.. ranges];
        var path = GetRangeFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(_rangeCache, JsonOptions));
    }

    public DateRange? MatchRange(DateTime date)
    {
        var d = DateOnly.FromDateTime(date);
        foreach (var dr in GetAllRanges())
        {
            if (d >= dr.StartDate && d <= dr.EndDate)
                return dr;
        }
        return null;
    }
}
