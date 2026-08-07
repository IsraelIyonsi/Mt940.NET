using Mt940.Tests.Support;

namespace Mt940.Tests.Reconciliation;

public sealed class CurrencyMismatchTests
{
    private static string BuildStatement(string openingCurrency, string closingCurrency)
    {
        return $"""
            :20:CURRENCY
            :25:1234567890
            :28C:1
            :60F:C260101{openingCurrency}100,00
            :62F:C260102{closingCurrency}100,00
            """;
    }

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
    public void A_third_character_currency_difference_is_flagged()
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

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Contains("EUR", warning.Message);
        Assert.Contains("EUP", warning.Message);
        Assert.Contains("currenc", warning.Message);
    }

    [Theory]
    [InlineData("USD", "USN")]
    [InlineData("USD", "USS")]
    [InlineData("CHF", "CHE")]
    [InlineData("CHF", "CHW")]
    [InlineData("CNY", "CNH")]
    public void Distinct_iso_currencies_sharing_a_prefix_are_flagged(string opening, string closing)
    {
        var file = Mt940Parser.Parse(BuildStatement(opening, closing));

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Contains(opening, warning.Message);
        Assert.Contains(closing, warning.Message);
        Assert.Contains("currenc", warning.Message);
    }

    [Theory]
    [InlineData("USD", "USN")]
    [InlineData("CHF", "CHE")]
    [InlineData("CNY", "CNH")]
    public void Distinct_iso_currencies_sharing_a_prefix_warn_in_throw_mode(string opening, string closing)
    {
        var options = new Mt940Options { BalanceMismatchBehavior = BalanceMismatchBehavior.Throw };

        var file = Mt940Parser.Parse(BuildStatement(opening, closing), options);

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Contains(opening, warning.Message);
        Assert.Contains(closing, warning.Message);
    }

    [Fact]
    public void Identical_currency_reconciles_cleanly()
    {
        const string text = """
            :20:SAMECURRENCY
            :25:1234567890
            :28C:1
            :60F:C260101USD100,00
            :61:2601020102D40,00NTRFNONREF
            :62F:C260102USD60,00
            """;

        var file = Mt940Parser.Parse(text);

        Assert.False(file.Report.HasWarnings);
        Assert.Equal(60.00m, Assert.Single(file.Statements).ClosingBalance!.SignedAmount);
    }

    [Fact]
    public void Amount_comparison_still_runs_when_currencies_match()
    {
        const string text = """
            :20:SAMECURRENCY2
            :25:1234567890
            :28C:1
            :60F:C260101USD100,00
            :62F:C260102USD90,00
            """;

        var file = Mt940Parser.Parse(text);

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Contains("reconcile", warning.Message);
    }
}
