namespace PhotoOrganiser.Models;

public record DateRange
{
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
}
