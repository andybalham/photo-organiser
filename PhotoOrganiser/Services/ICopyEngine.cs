using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public interface ICopyEngine
{
    Task<CopyResult> CopyAsync(
        IReadOnlyList<FileCandidate> files,
        IProgress<CopyProgress> progress,
        CancellationToken ct);
}
