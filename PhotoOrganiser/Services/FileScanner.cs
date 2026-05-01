using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;
using PhotoOrganiser.Helpers;
using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public class FileScanner : IFileScanner
{
    private static readonly DateTime MinValidDate = new DateTime(1900, 1, 1);

    public async Task<ScanResult> ScanAsync(string sourceFolder, string destinationFolder, CancellationToken ct)
    {
        var result = new ScanResult();

        var files = System.IO.Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories)
            .Where(f => FileTypes.IsSupported(Path.GetExtension(f)));

        await Task.Run(() =>
        {
            foreach (var filePath in files)
            {
                ct.ThrowIfCancellationRequested();

                var (date, dateSource) = ResolveDateOverride(filePath);
                var fileName = Path.GetFileName(filePath);
                string destFolder;

                if (dateSource == DateSource.Undated)
                {
                    destFolder = "Undated";
                }
                else
                {
                    destFolder = Path.Combine(
                        date.Year.ToString(),
                        $"{date.Month:D2} {date.ToString("MMMM")}");
                }

                var destPath = Path.Combine(destinationFolder, destFolder, fileName);

                var candidate = BuildCandidate(filePath, fileName, date, dateSource, destFolder, destPath);

                if (dateSource == DateSource.Undated)
                    result.Undated.Add(candidate);

                // skip = dest exists with same size; conflict = dest exists different size; otherwise copy
                var destExists = File.Exists(destPath);
                if (!candidate.ConflictExists && destExists && new FileInfo(candidate.SourcePath).Length == new FileInfo(destPath).Length)
                    result.ToSkip.Add(candidate);
                else
                    result.ToCopy.Add(candidate);
            }
        }, ct);

        return result;
    }

    private static FileCandidate BuildCandidate(
        string sourcePath, string fileName, DateTime date, DateSource dateSource,
        string destFolder, string destPath)
    {
        bool conflict = false;

        if (File.Exists(destPath))
        {
            var srcLen = new FileInfo(sourcePath).Length;
            var dstLen = new FileInfo(destPath).Length;
            if (srcLen != dstLen)
                conflict = true;
        }

        return new FileCandidate
        {
            SourcePath = sourcePath,
            FileName = fileName,
            OrganiseDate = date,
            DateSource = dateSource,
            DestinationFolder = destFolder,
            DestinationPath = destPath,
            ConflictExists = conflict,
        };
    }

    protected virtual (DateTime date, DateSource source) ResolveDateOverride(string filePath)
        => ResolveDate(filePath);

    private static (DateTime date, DateSource source) ResolveDate(string filePath)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(filePath);

            // Try EXIF DateTimeOriginal
            var exifSub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSub != null && exifSub.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dto) && dto >= MinValidDate)
                return (dto, DateSource.Exif);

            // Try EXIF DateTime
            var exifIfd = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifIfd != null && exifIfd.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dt) && dt >= MinValidDate)
                return (dt, DateSource.Exif);

            // Try QuickTime (videos)
            var qt = directories.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            if (qt != null && qt.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var qtDate) && qtDate >= MinValidDate)
                return (qtDate, DateSource.Exif);
        }
        catch
        {
            // MetadataExtractor throws on unreadable files — fall through
        }

        // Fallback: file creation time
        var created = File.GetCreationTime(filePath);
        if (created >= MinValidDate)
            return (created, DateSource.FileCreation);

        return (DateTime.MinValue, DateSource.Undated);
    }
}
