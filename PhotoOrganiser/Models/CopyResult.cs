namespace PhotoOrganiser.Models;

public record CopyResult(int Copied, int Skipped, int Failed, List<string> Errors);
