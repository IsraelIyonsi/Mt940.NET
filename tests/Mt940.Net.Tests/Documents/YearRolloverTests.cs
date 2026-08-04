using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class YearRolloverTests
{
    private static Mt940File ParseFile()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.YearRollover));
        Assert.False(file.Report.HasWarnings);
        return file;
    }

    [Fact]
    public void Entry_date_in_december_on_a_january_statement_resolves_to_the_previous_year()
    {
        var line = ParseFile().Statements[0].Lines[0];

        Assert.Equal(new DateOnly(2026, 1, 2), line.ValueDate);
        Assert.Equal(new DateOnly(2025, 12, 31), line.EntryDate);
    }

    [Fact]
    public void Entry_date_in_the_same_month_keeps_the_value_date_year()
    {
        var line = ParseFile().Statements[0].Lines[1];

        Assert.Equal(new DateOnly(2026, 1, 2), line.EntryDate);
    }

    [Fact]
    public void Entry_date_in_january_on_a_december_statement_resolves_to_the_next_year()
    {
        var line = Assert.Single(ParseFile().Statements[1].Lines);

        Assert.Equal(new DateOnly(2025, 12, 31), line.ValueDate);
        Assert.Equal(new DateOnly(2026, 1, 2), line.EntryDate);
    }

    [Fact]
    public void Rollover_statements_still_reconcile()
    {
        var file = ParseFile();

        Assert.Equal(550.00m, file.Statements[0].ClosingBalance!.SignedAmount);
        Assert.Equal(125.00m, file.Statements[1].ClosingBalance!.SignedAmount);
    }
}
