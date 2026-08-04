using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

/// <summary>
/// Regression suite for the amount bug left open in the abandoned incumbent parsers:
/// amounts without a decimal separator ("1234"), with only a trailing comma ("1234,"),
/// and with only a leading comma (",50") must all parse exactly.
/// </summary>
public sealed class NoDecimalAmountTests
{
    private static Mt940File ParseFile()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.NoDecimalAmounts));
        Assert.False(file.Report.HasWarnings);
        return file;
    }

    [Fact]
    public void Amount_with_trailing_comma_parses_as_whole_number()
    {
        var line = ParseFile().Statements[0].Lines[0];

        Assert.Equal(1234m, line.Amount);
        Assert.Equal(-1234m, line.SignedAmount);
    }

    [Fact]
    public void Amount_without_any_comma_parses_as_whole_number()
    {
        var line = ParseFile().Statements[0].Lines[1];

        Assert.Equal(1234m, line.Amount);
        Assert.Equal("NTRF", line.TransactionType);
    }

    [Fact]
    public void Amount_with_leading_comma_parses_as_fraction()
    {
        var line = ParseFile().Statements[0].Lines[2];

        Assert.Equal(0.50m, line.Amount);
    }

    [Fact]
    public void Statement_with_odd_amounts_still_reconciles()
    {
        var statement = ParseFile().Statements[0];

        Assert.Equal(4999.50m, statement.ClosingBalance!.SignedAmount);
        Assert.Equal(
            statement.ClosingBalance.SignedAmount,
            statement.OpeningBalance!.SignedAmount + statement.TotalCredits - statement.TotalDebits);
    }

    [Fact]
    public void Zero_balance_with_trailing_comma_parses()
    {
        var statement = ParseFile().Statements[1];

        Assert.Equal(0m, statement.OpeningBalance!.Amount);
    }

    [Fact]
    public void Twelve_digit_amount_parses_exactly()
    {
        var statement = ParseFile().Statements[1];

        Assert.Equal(999999999999.99m, Assert.Single(statement.Lines).Amount);
        Assert.Equal(999999999999.99m, statement.ClosingBalance!.Amount);
    }
}
