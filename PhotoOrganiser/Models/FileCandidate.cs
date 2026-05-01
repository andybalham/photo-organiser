namespace PhotoOrganiser.Models;

public record FileCandidate
{
    public string SourcePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime OrganiseDate { get; init; }
    public DateSource DateSource { get; init; }
    public string DestinationFolder { get; init; } = string.Empty;
    public string DestinationPath { get; init; } = string.Empty;
    public bool ConflictExists { get; init; }
}
