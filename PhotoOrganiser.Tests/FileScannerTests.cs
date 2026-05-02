using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Tests;

public class FileScannerTests : IDisposable
{
    private readonly string _sourceDir;
    private readonly string _destDir;
    private readonly FileScanner _scanner = new();

    public FileScannerTests()
    {
        _sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _destDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_destDir);
    }

    public void Dispose()
    {
        Directory.Delete(_sourceDir, recursive: true);
        Directory.Delete(_destDir, recursive: true);
    }

    // Creates a minimal JPEG with a proper EXIF APP1 segment containing DateTimeOriginal.
    // TIFF layout: IFD0 has ExifIFD pointer (0x8769) → SubIFD has DateTimeOriginal (0x9003).
    private string CreateJpegWithExif(string name, DateTime exifDate)
    {
        var path = Path.Combine(_sourceDir, name);
        var tiff = BuildTiffWithDateTimeOriginal(exifDate.ToString("yyyy:MM:dd HH:mm:ss"));

        using var ms = new MemoryStream();
        // SOI
        ms.WriteByte(0xFF); ms.WriteByte(0xD8);
        // APP1 marker
        ms.WriteByte(0xFF); ms.WriteByte(0xE1);
        int app1PayloadLen = 2 + 6 + tiff.Length; // 2 for length field, 6 for "Exif\0\0"
        ms.WriteByte((byte)(app1PayloadLen >> 8));
        ms.WriteByte((byte)(app1PayloadLen & 0xFF));
        ms.Write("Exif\0\0"u8);
        ms.Write(tiff);
        // EOI
        ms.WriteByte(0xFF); ms.WriteByte(0xD9);

        File.WriteAllBytes(path, ms.ToArray());
        return path;
    }

    // Builds a little-endian TIFF:
    //   IFD0 (offset 8):  1 entry — ExifIFD pointer (0x8769) → SubIFD at offset 26
    //   SubIFD (offset 26): 1 entry — DateTimeOriginal (0x9003) → ASCII at offset 44
    //   ASCII data (offset 44): "yyyy:MM:dd HH:mm:ss\0" (20 bytes)
    private static byte[] BuildTiffWithDateTimeOriginal(string dateStr)
    {
        var dateBytes = System.Text.Encoding.ASCII.GetBytes(dateStr + "\0"); // 20 bytes
        const int ifd0Offset = 8;
        const int subIfdOffset = 26;  // 8 + 2 + 12 + 4 = 26
        const int dataOffset = 44;    // 26 + 2 + 12 + 4 = 44

        using var ms = new MemoryStream();
        // TIFF header
        ms.Write("II"u8);
        WriteU16(ms, 42);
        WriteU32(ms, ifd0Offset);

        // IFD0: 1 entry — ExifIFD pointer tag 0x8769, type 4 (LONG), count 1, value = subIfdOffset
        WriteU16(ms, 1);
        WriteU16(ms, 0x8769); WriteU16(ms, 4); WriteU32(ms, 1); WriteU32(ms, subIfdOffset);
        WriteU32(ms, 0); // next IFD

        // SubIFD: 1 entry — DateTimeOriginal tag 0x9003, type 2 (ASCII), count 20, value = dataOffset
        WriteU16(ms, 1);
        WriteU16(ms, 0x9003); WriteU16(ms, 2); WriteU32(ms, (uint)dateBytes.Length); WriteU32(ms, dataOffset);
        WriteU32(ms, 0); // next IFD

        // ASCII date data
        ms.Write(dateBytes);

        return ms.ToArray();
    }

    private static void WriteU16(Stream s, ushort v) { s.WriteByte((byte)(v & 0xFF)); s.WriteByte((byte)(v >> 8)); }
    private static void WriteU32(Stream s, uint v) { s.WriteByte((byte)(v & 0xFF)); s.WriteByte((byte)((v >> 8) & 0xFF)); s.WriteByte((byte)((v >> 16) & 0xFF)); s.WriteByte((byte)(v >> 24)); }

    private string CreatePlainJpeg(string name)
    {
        var path = Path.Combine(_sourceDir, name);
        // Minimal JPEG: SOI + EOI, no EXIF
        File.WriteAllBytes(path, new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
        return path;
    }

    private string CreateFile(string name, byte[]? content = null)
    {
        var path = Path.Combine(_sourceDir, name);
        File.WriteAllBytes(path, content ?? new byte[] { 0x01, 0x02, 0x03 });
        return path;
    }

    [Fact]
    public async Task ScanAsync_PrefersExifOverCreation()
    {
        var exifDate = new DateTime(2021, 8, 15, 10, 0, 0);
        CreateJpegWithExif("photo.jpg", exifDate);

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.ToCopy.Concat(result.ToSkip).Single();
        Assert.Equal(DateSource.Exif, candidate.DateSource);
        Assert.Equal(exifDate.Year, candidate.OrganiseDate.Year);
        Assert.Equal(exifDate.Month, candidate.OrganiseDate.Month);
        Assert.Equal(exifDate.Day, candidate.OrganiseDate.Day);
    }

    [Fact]
    public async Task ScanAsync_FallsBackToCreationDate()
    {
        CreatePlainJpeg("noexit.jpg");

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.ToCopy.Concat(result.ToSkip).Single();
        Assert.Equal(DateSource.FileCreation, candidate.DateSource);
        Assert.True(candidate.OrganiseDate >= new DateTime(1900, 1, 1));
    }

    [Fact]
    public async Task ScanAsync_PlacesUndatedFilesInUndatedFolder()
    {
        // A PNG with no EXIF whose creation time we'll force to pre-1900 via a mock approach.
        // Since we can't set File.GetCreationTime below 1900 on Windows, we test via
        // an unreadable/corrupted file where MetadataExtractor will throw AND creation time
        // is well past 1900 — so this actually tests the creation-date path is NOT undated.
        // To properly test Undated we subclass FileScanner with injected date resolver.
        // Instead: test using a file extension that causes MetadataExtractor to throw
        // and verify the fallback path works.
        // Real Undated test: use the internal helper via testable subclass.

        // Create a scanner where we can force undated
        var scanner = new TestableFileScanner(returnDate: null);
        CreatePlainJpeg("undated.jpg");

        var result = await scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.Undated.Single();
        Assert.Equal("Undated", candidate.DestinationFolder);
        Assert.Equal(DateSource.Undated, candidate.DateSource);
    }

    [Fact]
    public async Task ScanAsync_SkipsSameSizeExistingFile()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        CreateFile("photo.jpg", content);

        // Place identical file at destination path
        var destSubDir = Path.Combine(_destDir, DateTime.Now.Year.ToString());
        // We don't know exact path; easier to pre-place at the expected location.
        // Use creation date path since no EXIF in plain file.
        var created = File.GetCreationTime(Path.Combine(_sourceDir, "photo.jpg"));
        var destFolder = Path.Combine(_destDir,
            created.Year.ToString(),
            $"{created.Month:D2} {created.ToString("MMMM")}");
        Directory.CreateDirectory(destFolder);
        File.WriteAllBytes(Path.Combine(destFolder, "photo.jpg"), content);

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        Assert.Single(result.ToSkip);
        Assert.Empty(result.ToCopy);
    }

    [Fact]
    public async Task ScanAsync_RoutesDuplicateToDuplicatesSubfolder()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        CreateFile("photo.jpg", content);

        var created = File.GetCreationTime(Path.Combine(_sourceDir, "photo.jpg"));
        var destFolder = Path.Combine(_destDir,
            created.Year.ToString(),
            $"{created.Month:D2} {created.ToString("MMMM")}");
        Directory.CreateDirectory(destFolder);
        File.WriteAllBytes(Path.Combine(destFolder, "photo.jpg"), new byte[] { 9, 9 }); // different size

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var duplicate = result.ToCopy.Single();
        Assert.True(duplicate.IsDuplicate);
        Assert.Contains("Duplicates", duplicate.DestinationPath);
        Assert.Equal("photo.jpg", Path.GetFileName(duplicate.DestinationPath));
    }

    [Fact]
    public async Task ScanAsync_SkipsDuplicateWhenAlreadyInDuplicatesFolder()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        CreateFile("photo.jpg", content);

        var created = File.GetCreationTime(Path.Combine(_sourceDir, "photo.jpg"));
        var destFolder = Path.Combine(_destDir,
            created.Year.ToString(),
            $"{created.Month:D2} {created.ToString("MMMM")}");
        Directory.CreateDirectory(Path.Combine(destFolder, "Duplicates"));
        File.WriteAllBytes(Path.Combine(destFolder, "photo.jpg"), new byte[] { 9, 9 }); // different size — triggers duplicate routing
        File.WriteAllBytes(Path.Combine(destFolder, "Duplicates", "photo.jpg"), content); // same size as source

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        Assert.Empty(result.ToCopy);
        Assert.Single(result.ToSkip);
    }

    [Fact]
    public async Task ScanAsync_ExcludesUnsupportedExtensions()
    {
        File.WriteAllText(Path.Combine(_sourceDir, "readme.txt"), "hello");
        File.WriteAllText(Path.Combine(_sourceDir, "doc.docx"), "word");
        CreatePlainJpeg("photo.jpg");

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var allCandidates = result.ToCopy.Concat(result.ToSkip).Concat(result.Undated);
        Assert.All(allCandidates, c => Assert.NotEqual(".txt", Path.GetExtension(c.FileName)));
        Assert.All(allCandidates, c => Assert.NotEqual(".docx", Path.GetExtension(c.FileName)));
        Assert.Single(allCandidates.DistinctBy(c => c.SourcePath));
    }

    [Fact]
    public async Task ScanAsync_BuildsCorrectDestinationPath()
    {
        var exifDate = new DateTime(2021, 8, 15);
        CreateJpegWithExif("photo.jpg", exifDate);

        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.ToCopy.Concat(result.ToSkip).Single();
        Assert.Equal(Path.Combine("2021", "08 August"), candidate.DestinationFolder);
        Assert.Equal(Path.Combine(_destDir, "2021", "08 August", "photo.jpg"), candidate.DestinationPath);
    }

    [Fact]
    public async Task ScanAsync_DateBefore1900TreatedAsUndated()
    {
        var scanner = new TestableFileScanner(returnDate: new DateTime(1800, 1, 1));
        CreatePlainJpeg("old.jpg");

        var result = await scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.Undated.Single();
        Assert.Equal(DateSource.Undated, candidate.DateSource);
        Assert.Equal("Undated", candidate.DestinationFolder);
    }

    [Fact]
    public async Task ScanAsync_EmptySourceFolder_ReturnsEmptyResult()
    {
        var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        Assert.Empty(result.ToCopy);
        Assert.Empty(result.ToSkip);
        Assert.Empty(result.Undated);
    }

    [Fact]
    public async Task ScanAsync_SpecialDate_RoutesToSubfolder()
    {
        var date = new DateTime(2023, 12, 25);
        var sd   = new SpecialDate { Name = "Xmas", Month = 12, Day = 25 };
        var svc  = new InMemorySpecialDateService([sd]);
        var scanner = new TestableFileScanner(date, svc);
        CreatePlainJpeg("photo.jpg");

        var result = await scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.ToCopy.Concat(result.ToSkip).Single();
        Assert.Equal(Path.Combine("2023", "12 December", "25 Xmas"), candidate.DestinationFolder);
        Assert.Equal(Path.Combine(_destDir, "2023", "12 December", "25 Xmas", "photo.jpg"), candidate.DestinationPath);
    }

    [Fact]
    public async Task ScanAsync_SpecialDate_NoMatch_UsesNormalPath()
    {
        var date = new DateTime(2023, 12, 26);
        var sd   = new SpecialDate { Name = "Xmas", Month = 12, Day = 25 };
        var svc  = new InMemorySpecialDateService([sd]);
        var scanner = new TestableFileScanner(date, svc);
        CreatePlainJpeg("photo.jpg");

        var result = await scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.ToCopy.Concat(result.ToSkip).Single();
        Assert.Equal(Path.Combine("2023", "12 December"), candidate.DestinationFolder);
    }

    [Fact]
    public async Task ScanAsync_SpecialDate_OneOff_MatchesOnlyCorrectYear()
    {
        var sd  = new SpecialDate { Name = "Wedding", Month = 8, Day = 20, Year = 2019 };
        var svc = new InMemorySpecialDateService([sd]);

        var scannerMatch    = new TestableFileScanner(new DateTime(2019, 8, 20), svc);
        var scannerNoMatch  = new TestableFileScanner(new DateTime(2020, 8, 20), svc);

        CreatePlainJpeg("photo.jpg");
        var match   = await scannerMatch.ScanAsync(_sourceDir, _destDir, CancellationToken.None);
        var noMatch = await scannerNoMatch.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var c1 = match.ToCopy.Concat(match.ToSkip).Single();
        Assert.Contains("20 Wedding", c1.DestinationFolder);

        var c2 = noMatch.ToCopy.Concat(noMatch.ToSkip).Single();
        Assert.DoesNotContain("Wedding", c2.DestinationFolder);
    }

    [Fact]
    public async Task ScanAsync_SpecialDate_Undated_NotRouted()
    {
        var sd  = new SpecialDate { Name = "Xmas", Month = 12, Day = 25 };
        var svc = new InMemorySpecialDateService([sd]);
        var scanner = new TestableFileScanner(returnDate: null, svc);
        CreatePlainJpeg("photo.jpg");

        var result = await scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.Undated.Single();
        Assert.Equal("Undated", candidate.DestinationFolder);
    }

    [Fact]
    public async Task ScanAsync_SpecialDate_NoService_UsesNormalPath()
    {
        var date    = new DateTime(2023, 12, 25);
        var scanner = new TestableFileScanner(date, specialDates: null);
        CreatePlainJpeg("photo.jpg");

        var result = await scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

        var candidate = result.ToCopy.Concat(result.ToSkip).Single();
        Assert.Equal(Path.Combine("2023", "12 December"), candidate.DestinationFolder);
    }

    [Fact]
    public async Task ScanAsync_InaccessibleSubfolder_LoggedAndScanContinues()
    {
        CreatePlainJpeg("accessible.jpg");

        var lockedDir = Path.Combine(_sourceDir, "locked");
        Directory.CreateDirectory(lockedDir);

        var di = new DirectoryInfo(lockedDir);
        var acl = di.GetAccessControl();
        var rule = new System.Security.AccessControl.FileSystemAccessRule(
            System.Security.Principal.WindowsIdentity.GetCurrent().Name,
            System.Security.AccessControl.FileSystemRights.ListDirectory,
            System.Security.AccessControl.AccessControlType.Deny);
        acl.AddAccessRule(rule);
        di.SetAccessControl(acl);

        try
        {
            var result = await _scanner.ScanAsync(_sourceDir, _destDir, CancellationToken.None);

            Assert.Single(result.ToCopy.Concat(result.ToSkip).Concat(result.Undated).DistinctBy(c => c.SourcePath));
            Assert.Contains(lockedDir, result.InaccessibleFolders);
        }
        finally
        {
            acl.RemoveAccessRule(rule);
            di.SetAccessControl(acl);
        }
    }
}

// Testable subclass that injects a fixed resolved date (bypasses MetadataExtractor + File.GetCreationTime)
internal class TestableFileScanner : FileScanner
{
    private readonly DateTime? _fixedDate;

    public TestableFileScanner(DateTime? returnDate, ISpecialDateService? specialDates = null)
        : base(specialDates) => _fixedDate = returnDate;

    protected override (DateTime date, DateSource source) ResolveDateOverride(string filePath)
    {
        if (_fixedDate == null)
            return (DateTime.MinValue, DateSource.Undated);
        if (_fixedDate.Value < new DateTime(1900, 1, 1))
            return (_fixedDate.Value, DateSource.Undated);
        return (_fixedDate.Value, DateSource.Exif);
    }
}
