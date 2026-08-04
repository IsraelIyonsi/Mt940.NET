using Mt940.Tests.Support;

namespace Mt940.Tests.Reconciliation;

public sealed class CurrencyMismatchTests
{
    [Fact]
    public void Opening_and_closing_in_different_currencies_warn_instead_of_reconciling()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.CurrencyFlip));

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("62F", warning.Tag);
        Assert.Equal(0, warning.StatementIndex);
        Assert.Contains("EUR", warning.Message);
        Assert.Contains("USD", warning.Message);
        Assert.Contains("currenc", warning.Message);
    }

    [Fact]
    public void Currency_mismatch_warns_even_when_balance_mismatch_is_set_to_throw()
    {
        var options = new Mt940Options { BalanceMismatchBehavior = BalanceMismatchBehavior.Throw };

        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.CurrencyFlip), options);

        var statement = Assert.Single(file.Statements);
        Assert.Equal("EUR", statement.OpeningBalance!.Currency);
        Assert.Equal("USD", statement.ClosingBalance!.Currency);
        Assert.Single(file.Report.Warnings);
    }

    [Fact]
    public void Only_the_first_two_currency_characters_must_match()
    {
        const string text = """
            :20:THIRDLETTER
            :25:1234567890
            :28C:1
            :60F:C260101EUR100,00
            :61:2601020102D40,00NTRFNONREF
            :62F:C260102EUP60,00
            """;

        var file = Mt940Parser.Parse(text);

        Assert.False(file.Report.HasWarnings);
        Assert.Equal(60.00m, Assert.Single(file.Statements).ClosingBalance!.SignedAmount);
    }

    [Fact]
    public void Amount_comparison_still_runs_when_the_first_two_characters_match()
    {
        const string text = """
            :20:THIRDLETTER2
            :25:1234567890
            :28C:1
            :60F:C260101EUR100,00
            :62F:C260102EUP90,00
            """;

        var file = Mt940Parser.Parse(text);

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Contains("reconcile", warning.Message);
    }
}
