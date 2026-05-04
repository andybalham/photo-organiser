using PhotoOrganiser.Helpers;
using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public class CopyEngine : ICopyEngine
{
    public async Task<CopyResult> CopyAsync(
        IReadOnlyList<FileCandidate> files,
        IProgress<CopyProgress> progress,
        CancellationToken ct)
    {
        int copied = 0, skipped = 0, failed = 0;
        var errors = new List<string>();

        for (int i = 0; i < files.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var file = files[i];
            progress.Report(new CopyProgress(i, files.Count, file.FileName));

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file.DestinationPath)!);

                if (File.Exists(file.DestinationPath))
                {
                    if (new FileInfo(file.SourcePath).Length == new FileInfo(file.DestinationPath).Length)
                    {
                        skipped++;
                        continue;
                    }

                    // Different size — redirect to Duplicates/
                    var dupDir = Path.Combine(Path.GetDirectoryName(file.DestinationPath)!, "Duplicates");
                    Directory.CreateDirectory(dupDir);
                    var dupBase = Path.Combine(dupDir, file.FileName);
                    var dupPath = FileNameHelper.GetUniqueDestinationPath(dupBase, File.Exists);
                    File.Copy(file.SourcePath, dupPath, overwrite: false);
                    var srcInfoDup = new FileInfo(file.SourcePath);
                    File.SetCreationTime(dupPath, srcInfoDup.CreationTime);
                    File.SetLastWriteTime(dupPath, srcInfoDup.LastWriteTime);
                    copied++;
                    continue;
                }

                File.Copy(file.SourcePath, file.DestinationPath, overwrite: false);

                var srcInfo = new FileInfo(file.SourcePath);
                File.SetCreationTime(file.DestinationPath, srcInfo.CreationTime);
                File.SetLastWriteTime(file.DestinationPath, srcInfo.LastWriteTime);

                copied++;
            }
            catch (IOException ex)
            {
                failed++;
                errors.Add($"{file.FileName}: {ex.Message}");
            }
        }

        progress.Report(new CopyProgress(files.Count, files.Count, string.Empty));
        return await Task.FromResult(new CopyResult(copied, skipped, failed, errors));
    }
}
