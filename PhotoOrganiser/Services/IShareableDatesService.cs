using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public interface IShareableDatesService
{
    string? LinkedFilePath { get; }

    void LinkFile(string path, IEnumerable<SpecialDate> currentDates, IEnumerable<Models.DateRange> currentRanges);
    void SaveToFile(string path, IEnumerable<SpecialDate> dates, IEnumerable<Models.DateRange> ranges);
    void Unlink();

    (IReadOnlyList<SpecialDate> SpecialDates, IReadOnlyList<Models.DateRange> DateRanges)? TryAutoLoad();

    void AutoSave(IEnumerable<SpecialDate> dates, IEnumerable<Models.DateRange> ranges, Action<string> onError);
}
