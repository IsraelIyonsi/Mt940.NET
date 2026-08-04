using Mt940.Internal;

namespace Mt940.Tests.Lines;

public sealed class EntryDateResolutionTests
{
    [Theory]
    [InlineData(2026, 1, 2, "1231", 2025, 12, 31)]
    [InlineData(2025, 12, 31, "0102", 2026, 1, 2)]
    [InlineData(2026, 7, 2, "0702", 2026, 7, 2)]
    [InlineData(2026, 6, 15, "0616", 2026, 6, 16)]
    [InlineData(2026, 1, 1, "0101", 2026, 1, 1)]
    [InlineData(2024, 3, 1, "0229", 2024, 2, 29)]
    [InlineData(2026, 12, 31, "1230", 2026, 12, 30)]
    public void Entry_date_resolves_to_the_year_nearest_the_value_date(
        int valueYear, int valueMonth, int valueDay, string monthDay,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        var resolved = FieldParsers.ResolveEntryDate(
            new DateOnly(valueYear, valueMonth, valueDay), monthDay, lineNumber: 1);

        Assert.Equal(new DateOnly(expectedYear, expectedMonth, expectedDay), resolved);
    }

    [Theory]
    [InlineData("0229")]
    [InlineData("1332")]
    [InlineData("0000")]
    public void Impossible_entry_dates_throw(string monthDay)
    {
        Assert.Throws<Mt940ParseException>(
            () => FieldParsers.ResolveEntryDate(new DateOnly(2026, 2, 28), monthDay, lineNumber: 1));
    }
}
