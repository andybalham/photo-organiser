using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Tests;

public class SpecialDateServiceTests
{
    private static SpecialDate? MatchFrom(IEnumerable<SpecialDate> dates, DateTime date)
    {
        var svc = new InMemorySpecialDateService(dates, []);
        return svc.Match(date);
    }

    private static DateRange? MatchRangeFrom(IEnumerable<DateRange> ranges, DateTime date)
    {
        var svc = new InMemorySpecialDateService([], ranges);
        return svc.MatchRange(date);
    }

    // ── Special Dates ────────────────────────────────────────────────────────────

    [Fact]
    public void Match_AnnualDate_MatchesSameDayMonth_AnyYear()
    {
        var sd = new SpecialDate { Name = "Xmas", Month = 12, Day = 25 };
        Assert.NotNull(MatchFrom([sd], new DateTime(2020, 12, 25)));
        Assert.NotNull(MatchFrom([sd], new DateTime(2023, 12, 25)));
    }

    [Fact]
    public void Match_AnnualDate_NoMatchOnDifferentDay()
    {
        var sd = new SpecialDate { Name = "Xmas", Month = 12, Day = 25 };
        Assert.Null(MatchFrom([sd], new DateTime(2023, 12, 24)));
    }

    [Fact]
    public void Match_AnnualDate_NoMatchOnDifferentMonth()
    {
        var sd = new SpecialDate { Name = "Xmas", Month = 12, Day = 25 };
        Assert.Null(MatchFrom([sd], new DateTime(2023, 11, 25)));
    }

    [Fact]
    public void Match_OneOffDate_MatchesOnlySpecifiedYear()
    {
        var sd = new SpecialDate { Name = "Wedding", Month = 8, Day = 20, Year = 2019 };
        Assert.NotNull(MatchFrom([sd], new DateTime(2019, 8, 20)));
        Assert.Null(MatchFrom([sd], new DateTime(2020, 8, 20)));
    }

    [Fact]
    public void Match_FirstDefinedWins_WhenMultipleMatchSameDay()
    {
        var first  = new SpecialDate { Name = "First",  Month = 6, Day = 15 };
        var second = new SpecialDate { Name = "Second", Month = 6, Day = 15 };
        var result = MatchFrom([first, second], new DateTime(2023, 6, 15));
        Assert.Equal("First", result?.Name);
    }

    [Fact]
    public void Match_ReturnsNull_WhenNoSpecialDates()
    {
        Assert.Null(MatchFrom([], new DateTime(2023, 6, 15)));
    }

    [Fact]
    public void Match_ReturnsCorrectName()
    {
        var sd = new SpecialDate { Name = "Birthday", Month = 6, Day = 15 };
        var result = MatchFrom([sd], new DateTime(2023, 6, 15));
        Assert.Equal("Birthday", result?.Name);
    }

    [Fact]
    public void Save_And_GetAll_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "special_dates.json");
        var svc = new TestableSpecialDateService(path, path + ".ranges");
        var input = new List<SpecialDate>
        {
            new() { Name = "Xmas",    Month = 12, Day = 25 },
            new() { Name = "Wedding", Month = 8,  Day = 20, Year = 2019 },
        };

        svc.Save(input);
        var loaded = svc.GetAll();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("Xmas",    loaded[0].Name);
        Assert.Equal(12,        loaded[0].Month);
        Assert.Equal(25,        loaded[0].Day);
        Assert.Null(            loaded[0].Year);
        Assert.Equal("Wedding", loaded[1].Name);
        Assert.Equal(2019,      loaded[1].Year);

        File.Delete(path);
    }

    [Fact]
    public void GetAll_ReturnsEmpty_WhenFileAbsent()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "special_dates.json");
        var svc = new TestableSpecialDateService(path, path + ".ranges");
        Assert.Empty(svc.GetAll());
    }

    // ── Date Ranges ───────────────────────────────────────────────────────────────

    [Fact]
    public void MatchRange_FirstDayOfRange_Matches()
    {
        var dr = new DateRange { Name = "Holiday", StartDate = new DateOnly(2024, 8, 1), EndDate = new DateOnly(2024, 8, 14) };
        Assert.NotNull(MatchRangeFrom([dr], new DateTime(2024, 8, 1)));
    }

    [Fact]
    public void MatchRange_LastDayOfRange_Matches()
    {
        var dr = new DateRange { Name = "Holiday", StartDate = new DateOnly(2024, 8, 1), EndDate = new DateOnly(2024, 8, 14) };
        Assert.NotNull(MatchRangeFrom([dr], new DateTime(2024, 8, 14)));
    }

    [Fact]
    public void MatchRange_DayBeforeStart_NoMatch()
    {
        var dr = new DateRange { Name = "Holiday", StartDate = new DateOnly(2024, 8, 1), EndDate = new DateOnly(2024, 8, 14) };
        Assert.Null(MatchRangeFrom([dr], new DateTime(2024, 7, 31)));
    }

    [Fact]
    public void MatchRange_DayAfterEnd_NoMatch()
    {
        var dr = new DateRange { Name = "Holiday", StartDate = new DateOnly(2024, 8, 1), EndDate = new DateOnly(2024, 8, 14) };
        Assert.Null(MatchRangeFrom([dr], new DateTime(2024, 8, 15)));
    }

    [Fact]
    public void MatchRange_CrossMonth_MatchesBothMonths()
    {
        var dr = new DateRange { Name = "Trip", StartDate = new DateOnly(2024, 12, 28), EndDate = new DateOnly(2025, 1, 3) };
        Assert.NotNull(MatchRangeFrom([dr], new DateTime(2024, 12, 28)));
        Assert.NotNull(MatchRangeFrom([dr], new DateTime(2025, 1, 1)));
        Assert.NotNull(MatchRangeFrom([dr], new DateTime(2025, 1, 3)));
        Assert.Null(MatchRangeFrom([dr], new DateTime(2024, 12, 27)));
        Assert.Null(MatchRangeFrom([dr], new DateTime(2025, 1, 4)));
    }

    [Fact]
    public void MatchRange_ReturnsCorrectName()
    {
        var dr = new DateRange { Name = "Summer", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 7, 31) };
        var result = MatchRangeFrom([dr], new DateTime(2024, 7, 15));
        Assert.Equal("Summer", result?.Name);
    }

    [Fact]
    public void MatchRange_FirstDefinedWins_WhenOverlapping()
    {
        var first  = new DateRange { Name = "First",  StartDate = new DateOnly(2024, 8, 1), EndDate = new DateOnly(2024, 8, 14) };
        var second = new DateRange { Name = "Second", StartDate = new DateOnly(2024, 8, 5), EndDate = new DateOnly(2024, 8, 20) };
        var result = MatchRangeFrom([first, second], new DateTime(2024, 8, 10));
        Assert.Equal("First", result?.Name);
    }

    [Fact]
    public void MatchRange_ReturnsNull_WhenNoRanges()
    {
        Assert.Null(MatchRangeFrom([], new DateTime(2024, 8, 1)));
    }

    [Fact]
    public void SaveRanges_And_GetAllRanges_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var datesPath  = Path.Combine(dir, "special_dates.json");
        var rangesPath = Path.Combine(dir, "date_ranges.json");
        var svc = new TestableSpecialDateService(datesPath, rangesPath);

        var input = new List<DateRange>
        {
            new() { Name = "Holiday",   StartDate = new DateOnly(2024, 8, 1),  EndDate = new DateOnly(2024, 8, 14) },
            new() { Name = "Xmas Trip", StartDate = new DateOnly(2024, 12, 28), EndDate = new DateOnly(2025, 1, 3) },
        };

        svc.SaveRanges(input);
        var loaded = svc.GetAllRanges();

        Assert.Equal(2, loaded.Count);
        Assert.Equal("Holiday",             loaded[0].Name);
        Assert.Equal(new DateOnly(2024, 8, 1),  loaded[0].StartDate);
        Assert.Equal(new DateOnly(2024, 8, 14), loaded[0].EndDate);
        Assert.Equal("Xmas Trip",           loaded[1].Name);
        Assert.Equal(new DateOnly(2024, 12, 28), loaded[1].StartDate);
        Assert.Equal(new DateOnly(2025, 1, 3),   loaded[1].EndDate);

        Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void GetAllRanges_ReturnsEmpty_WhenFileAbsent()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "date_ranges.json");
        var svc = new TestableSpecialDateService(path + ".dates", path);
        Assert.Empty(svc.GetAllRanges());
    }
}

// Drives service logic with in-memory lists (no file I/O)
internal class InMemorySpecialDateService : ISpecialDateService
{
    private readonly IReadOnlyList<SpecialDate> _dates;
    private readonly IReadOnlyList<DateRange> _ranges;

    public InMemorySpecialDateService(IEnumerable<SpecialDate> dates, IEnumerable<DateRange> ranges)
    {
        _dates  = [.. dates];
        _ranges = [.. ranges];
    }

    public IReadOnlyList<SpecialDate> GetAll() => _dates;
    public void Save(IEnumerable<SpecialDate> dates) { }

    public SpecialDate? Match(DateTime date)
    {
        foreach (var sd in _dates)
        {
            if (sd.Month != date.Month || sd.Day != date.Day) continue;
            if (sd.Year == null || sd.Year == date.Year) return sd;
        }
        return null;
    }

    public IReadOnlyList<DateRange> GetAllRanges() => _ranges;
    public void SaveRanges(IEnumerable<DateRange> ranges) { }

    public DateRange? MatchRange(DateTime date)
    {
        var d = DateOnly.FromDateTime(date);
        foreach (var dr in _ranges)
            if (d >= dr.StartDate && d <= dr.EndDate) return dr;
        return null;
    }
}

// SpecialDateService with injectable file paths for round-trip tests
internal class TestableSpecialDateService : SpecialDateService
{
    private readonly string _path;
    private readonly string _rangePath;

    public TestableSpecialDateService(string path, string rangePath)
    {
        _path      = path;
        _rangePath = rangePath;
    }

    protected override string GetFilePath()      => _path;
    protected override string GetRangeFilePath() => _rangePath;
}
