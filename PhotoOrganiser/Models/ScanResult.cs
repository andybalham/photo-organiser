namespace PhotoOrganiser.Models;

public class ScanResult
{
    public List<FileCandidate> ToCopy { get; } = new();
    public List<FileCandidate> ToSkip { get; } = new();
    public List<FileCandidate> Undated { get; } = new();
}
