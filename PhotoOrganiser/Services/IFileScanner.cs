using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public interface IFileScanner
{
    Task<ScanResult> ScanAsync(string sourceFolder, string destinationFolder, CancellationToken ct);
}
