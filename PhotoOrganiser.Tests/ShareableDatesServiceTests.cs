using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Tests;

public class ShareableDatesServiceTests
{
    private sealed class TestableShareableDatesService : ShareableDatesService
    {
        private readonly string _settingsPath;
        protected override string GetSettingsPath() => _settingsPath;

        public TestableShareableDatesService(string tempDir) : base(false)
        {
            _settingsPath = Path.Combine(tempDir, "settings.json");
            Initialize();
        }
    }

    private static (TestableShareableDatesService svc, string dir) Create()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var svc = new TestableShareableDatesService(dir);
        return (svc, dir);
    }

    [Fact]
    public void LinkedFilePath_IsNull_WhenNoSettingsFile()
    {
        var (svc, _) = Create();
        Assert.Null(svc.LinkedFilePath);
    }

    [Fact]
    public void LinkFile_NewPath_WritesDataAndPersistsPath()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        var dates = new[] { new SpecialDate { Name = "Xmas", Month = 12, Day = 25 } };
        var ranges = Array.Empty<DateRange>();

        svc.LinkFile(filePath, dates, ranges);

        Assert.Equal(filePath, svc.LinkedFilePath);
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("Xmas", content);
    }

    [Fact]
    public void LinkFile_ExistingFile_DoesNotOverwrite()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        File.WriteAllText(filePath, """{"specialDates":[{"name":"Holiday","month":8,"day":1}],"dateRanges":[]}""");

        svc.LinkFile(filePath, [], []);

        var content = File.ReadAllText(filePath);
        Assert.Contains("Holiday", content);
    }

    [Fact]
    public void TryAutoLoad_ReturnsNull_WhenNoLinkedFile()
    {
        var (svc, _) = Create();
        Assert.Null(svc.TryAutoLoad());
    }

    [Fact]
    public void TryAutoLoad_ReturnsData_WhenLinkedFileExists()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        var dates = new[] { new SpecialDate { Name = "Xmas", Month = 12, Day = 25 } };
        var ranges = new[] { new DateRange { Name = "Hols", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 7, 14) } };

        svc.LinkFile(filePath, dates, ranges);
        var loaded = svc.TryAutoLoad();

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Value.SpecialDates);
        Assert.Equal("Xmas", loaded.Value.SpecialDates[0].Name);
        Assert.Single(loaded.Value.DateRanges);
        Assert.Equal("Hols", loaded.Value.DateRanges[0].Name);
    }

    [Fact]
    public void TryAutoLoad_ClearsLinkedPath_WhenFileMissing()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        svc.LinkFile(filePath, [], []);
        File.Delete(filePath);

        var result = svc.TryAutoLoad();

        Assert.Null(result);
        Assert.Null(svc.LinkedFilePath);
    }

    [Fact]
    public void Unlink_ClearsLinkedPath()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        svc.LinkFile(filePath, [], []);
        Assert.NotNull(svc.LinkedFilePath);

        svc.Unlink();

        Assert.Null(svc.LinkedFilePath);
    }

    [Fact]
    public void AutoSave_WritesCurrentData_WhenLinked()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        svc.LinkFile(filePath, [], []);

        var dates = new[] { new SpecialDate { Name = "Birthday", Month = 6, Day = 15 } };
        svc.AutoSave(dates, [], _ => { });

        var content = File.ReadAllText(filePath);
        Assert.Contains("Birthday", content);
    }

    [Fact]
    public void AutoSave_DoesNothing_WhenNotLinked()
    {
        var (svc, dir) = Create();
        var called = false;
        svc.AutoSave([], [], _ => called = true);
        Assert.False(called);
    }

    [Fact]
    public void AutoSave_CallsOnError_WhenWriteFails()
    {
        var (svc, dir) = Create();
        var badPath = Path.Combine(dir, "no_such_dir", "dates.dates.json");
        // Manually set linked path to bad path via Link with empty existing check skipped
        // Use a pre-existing file path that we can write then make read-only to force failure
        var filePath = Path.Combine(dir, "dates.dates.json");
        svc.LinkFile(filePath, [], []);
        File.WriteAllText(filePath, "{}");
        File.SetAttributes(filePath, FileAttributes.ReadOnly);

        string? errorMsg = null;
        try
        {
            svc.AutoSave([], [], msg => errorMsg = msg);
        }
        finally
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        Assert.NotNull(errorMsg);
    }

    [Fact]
    public void LinkedPath_PersistedAcrossInstances()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        svc.LinkFile(filePath, [], []);

        var svc2 = new TestableShareableDatesService(dir);
        Assert.Equal(filePath, svc2.LinkedFilePath);
    }

    [Fact]
    public void Unlink_PersistedAcrossInstances()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        svc.LinkFile(filePath, [], []);
        svc.Unlink();

        var svc2 = new TestableShareableDatesService(dir);
        Assert.Null(svc2.LinkedFilePath);
    }

    [Fact]
    public void SaveToFile_WritesData_WithoutChangingLinkedPath()
    {
        var (svc, dir) = Create();
        var linkedPath = Path.Combine(dir, "linked.dates.json");
        svc.LinkFile(linkedPath, [], []);

        var savePath = Path.Combine(dir, "export.dates.json");
        var dates = new[] { new SpecialDate { Name = "Export", Month = 3, Day = 1 } };
        svc.SaveToFile(savePath, dates, []);

        Assert.True(File.Exists(savePath));
        Assert.Contains("Export", File.ReadAllText(savePath));
        Assert.Equal(linkedPath, svc.LinkedFilePath);
    }

    [Fact]
    public void SaveToFile_DoesNotRequireLinkedFile()
    {
        var (svc, dir) = Create();
        var savePath = Path.Combine(dir, "export.dates.json");
        svc.SaveToFile(savePath, [], []);
        Assert.True(File.Exists(savePath));
    }

    [Fact]
    public void RoundTrip_BothCollections()
    {
        var (svc, dir) = Create();
        var filePath = Path.Combine(dir, "dates.dates.json");
        var dates = new[]
        {
            new SpecialDate { Name = "Xmas", Month = 12, Day = 25 },
            new SpecialDate { Name = "Wedding", Month = 8, Day = 20, Year = 2019 },
        };
        var ranges = new[]
        {
            new DateRange { Name = "Summer", StartDate = new DateOnly(2024, 7, 1), EndDate = new DateOnly(2024, 8, 31) },
        };

        svc.LinkFile(filePath, dates, ranges);
        var loaded = svc.TryAutoLoad();

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.Value.SpecialDates.Count);
        Assert.Single(loaded.Value.DateRanges);
        Assert.Equal(2019, loaded.Value.SpecialDates[1].Year);
        Assert.Equal(new DateOnly(2024, 8, 31), loaded.Value.DateRanges[0].EndDate);
    }
}
