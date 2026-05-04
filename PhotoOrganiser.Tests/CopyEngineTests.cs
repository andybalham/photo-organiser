using PhotoOrganiser.Models;
using PhotoOrganiser.Services;

namespace PhotoOrganiser.Tests;

public class CopyEngineTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public CopyEngineTests() => Directory.CreateDirectory(_tmp);

    public void Dispose()
    {
        if (Directory.Exists(_tmp))
            Directory.Delete(_tmp, recursive: true);
    }

    private string MakeFile(string subdir, string name, string content = "data")
    {
        var dir = Path.Combine(_tmp, subdir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private FileCandidate MakeCandidate(string srcPath, string destPath) => new()
    {
        SourcePath      = srcPath,
        FileName        = Path.GetFileName(srcPath),
        DestinationPath = destPath,
    };

    private static async Task<CopyResult> Run(
        ICopyEngine engine,
        IReadOnlyList<FileCandidate> files,
        CancellationToken ct = default)
    {
        var progress = new Progress<CopyProgress>(_ => { });
        return await engine.CopyAsync(files, progress, ct);
    }

    [Fact]
    public async Task CopyAsync_CopiesFileToDestination()
    {
        var src  = MakeFile("src", "a.jpg");
        var dest = Path.Combine(_tmp, "dest", "a.jpg");
        var engine = new CopyEngine();

        var result = await Run(engine, [MakeCandidate(src, dest)]);

        Assert.Equal(1, result.Copied);
        Assert.True(File.Exists(dest));
    }

    [Fact]
    public async Task CopyAsync_CreatesDestinationDirectory()
    {
        var src  = MakeFile("src", "b.jpg");
        var dest = Path.Combine(_tmp, "deep", "2021", "08 August", "b.jpg");
        var engine = new CopyEngine();

        await Run(engine, [MakeCandidate(src, dest)]);

        Assert.True(File.Exists(dest));
    }

    [Fact]
    public async Task CopyAsync_PreservesSourceTimestamps()
    {
        var src = MakeFile("src", "ts.jpg");
        var stamp = new DateTime(2021, 8, 15, 12, 0, 0, DateTimeKind.Local);
        File.SetCreationTime(src, stamp);
        File.SetLastWriteTime(src, stamp);

        var dest   = Path.Combine(_tmp, "dest", "ts.jpg");
        var engine = new CopyEngine();

        await Run(engine, [MakeCandidate(src, dest)]);

        Assert.Equal(stamp, File.GetCreationTime(dest));
        Assert.Equal(stamp, File.GetLastWriteTime(dest));
    }

    [Fact]
    public async Task CopyAsync_LogsErrorAndContinuesOnIOException()
    {
        var src1 = MakeFile("src", "good.jpg");
        // src2 has different content/size from existing dest → routes to Duplicates/, not an error
        var src2 = MakeFile("src", "bad.jpg", "data");

        var destDir = Path.Combine(_tmp, "dest");
        Directory.CreateDirectory(destDir);

        var destBad = Path.Combine(destDir, "bad.jpg");
        File.WriteAllText(destBad, "existing content"); // different size → Duplicates

        var destGood = Path.Combine(destDir, "good.jpg");
        var engine   = new CopyEngine();

        var result = await Run(engine, [
            MakeCandidate(src2, destBad),   // different size → copied to Duplicates/
            MakeCandidate(src1, destGood),  // normal copy
        ]);

        Assert.Equal(2, result.Copied);
        Assert.Equal(0, result.Failed);
        Assert.True(File.Exists(destGood));
        Assert.True(Directory.Exists(Path.Combine(destDir, "Duplicates")));
    }

    [Fact]
    public async Task CopyAsync_ContinuesAfterIOException()
    {
        // Force a real IOException by making src unreadable via missing file
        var src1 = MakeFile("src", "good.jpg");
        var destDir = Path.Combine(_tmp, "dest");
        var destMissing = Path.Combine(destDir, "missing.jpg");
        var destGood    = Path.Combine(destDir, "good.jpg");
        var engine = new CopyEngine();

        var result = await Run(engine, [
            new FileCandidate { SourcePath = Path.Combine(_tmp, "nonexistent.jpg"), FileName = "missing.jpg", DestinationPath = destMissing },
            MakeCandidate(src1, destGood),
        ]);

        Assert.Equal(1, result.Copied);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
        Assert.True(File.Exists(destGood));
    }

    [Fact]
    public async Task CopyAsync_HonoursCancellation()
    {
        var src1 = MakeFile("src", "c1.jpg");
        var src2 = MakeFile("src", "c2.jpg");
        var dest1 = Path.Combine(_tmp, "dest", "c1.jpg");
        var dest2 = Path.Combine(_tmp, "dest", "c2.jpg");

        using var cts = new CancellationTokenSource();
        var engine = new CopyEngine();

        var files = (IReadOnlyList<FileCandidate>)[
            MakeCandidate(src1, dest1),
            MakeCandidate(src2, dest2),
        ];

        cts.Cancel(); // cancel before start

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => engine.CopyAsync(files, new Progress<CopyProgress>(_ => { }), cts.Token));
    }

    [Fact]
    public async Task CopyAsync_ReportsProgressPerFile()
    {
        var reports = new List<CopyProgress>();
        var progress = new Progress<CopyProgress>(p => reports.Add(p));

        var src1 = MakeFile("src", "p1.jpg");
        var src2 = MakeFile("src", "p2.jpg");

        var engine = new CopyEngine();
        await engine.CopyAsync(
            [
                MakeCandidate(src1, Path.Combine(_tmp, "dest", "p1.jpg")),
                MakeCandidate(src2, Path.Combine(_tmp, "dest", "p2.jpg")),
            ],
            progress,
            CancellationToken.None);

        // Progress<T> fires on the sync context; give it a tick to flush
        await Task.Delay(50);

        // Expect at least one report per file plus final completion report
        Assert.True(reports.Count >= 2);
        Assert.Contains(reports, r => r.Completed == 2 && r.Total == 2);
    }

    [Fact]
    public async Task CopyAsync_NeverOverwrites()
    {
        // Same size → silent skip; dest must remain unchanged
        var src  = MakeFile("src", "dup.jpg", "data");
        var dest = Path.Combine(_tmp, "dest", "dup.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.WriteAllText(dest, "data"); // same content/size

        var engine = new CopyEngine();
        var result = await Run(engine, [MakeCandidate(src, dest)]);

        Assert.Equal(0, result.Failed);
        Assert.Equal("data", File.ReadAllText(dest)); // dest unchanged
    }
}
