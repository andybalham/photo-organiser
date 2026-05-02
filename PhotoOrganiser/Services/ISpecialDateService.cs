using PhotoOrganiser.Models;

namespace PhotoOrganiser.Services;

public interface ISpecialDateService
{
    IReadOnlyList<SpecialDate> GetAll();
    void Save(IEnumerable<SpecialDate> dates);
    SpecialDate? Match(DateTime date);
}
