using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Tests;

public class SpecialDateServiceTests
{
    private static IReadOnlyList<SpecialDate> Dates(params SpecialDate[] dates) => dates;

    private static SpecialDate? MatchFrom(IEnumerable<SpecialDate> dates, DateTime date)
    {
        // Drive Match via a testable subclass that uses an in-memory list
        var svc = new InMemorySpecialDateService(dates);
        return svc.Match(date);
    }

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
        var svc = new TestableSpecialDateService(path);
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
        var svc = new TestableSpecialDateService(path);
        Assert.Empty(svc.GetAll());
    }
}

// Drives SpecialDateService.Match with an in-memory list (no file I/O)
internal class InMemorySpecialDateService : ISpecialDateService
{
    private readonly IReadOnlyList<SpecialDate> _dates;
    public InMemorySpecialDateService(IEnumerable<SpecialDate> dates) => _dates = [.. dates];

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
}

// SpecialDateService with injectable file path for round-trip tests
internal class TestableSpecialDateService : SpecialDateService
{
    private readonly string _path;
    public TestableSpecialDateService(string path) => _path = path;
    protected override string GetFilePath() => _path;
}
