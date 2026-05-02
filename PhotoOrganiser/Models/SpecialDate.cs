namespace PhotoOrganiser.Models;

public record SpecialDate
{
    public string Name { get; init; } = string.Empty;
    public int Month { get; init; }
    public int Day { get; init; }
    public int? Year { get; init; }
}
