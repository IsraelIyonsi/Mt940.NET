using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class WrappedStatementLineTests
{
    private static Mt940File ParseFile() =>
        Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Wrapped61));

    [Fact]
    public void Supplementary_details_starting_with_double_slash_are_kept_but_flagged()
    {
        var file = ParseFile();

        var line = Assert.Single(Assert.Single(file.Statements).Lines);
        Assert.Equal("VERYLONGCUSTREF1", line.CustomerReference);
        Assert.Null(line.BankReference);
        Assert.Equal("//BANKREF999", line.SupplementaryDetails);

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("61", warning.Tag);
        Assert.Contains("//", warning.Message);
    }

    [Fact]
    public void The_flagged_line_still_reconciles()
    {
        var statement = Assert.Single(ParseFile().Statements);

        Assert.Equal(100.00m, Assert.Single(statement.Lines).SignedAmount);
        Assert.Equal(100.00m, statement.ClosingBalance!.Amount);
    }

    [Fact]
    public void Ordinary_supplementary_details_do_not_warn()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Minimal));

        Assert.False(file.Report.HasWarnings);
    }
}
