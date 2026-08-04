using Mt940.Tests.Support;

namespace Mt940.Tests.Culture;

public sealed class CultureIndependenceTests
{
    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void Amounts_and_dates_parse_identically_under_any_culture(string cultureName)
    {
        TestFixtures.RunWithCulture(cultureName, () =>
        {
            var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Minimal));

            var statement = Assert.Single(file.Statements);
            Assert.Equal(1000.00m, statement.OpeningBalance!.Amount);
            Assert.Equal(750.00m, statement.ClosingBalance!.Amount);
            Assert.Equal(250.00m, Assert.Single(statement.Lines).Amount);
            Assert.Equal(new DateOnly(2026, 7, 2), statement.Lines[0].ValueDate);
        });
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    public void Mismatch_messages_use_invariant_decimal_formatting(string cultureName)
    {
        TestFixtures.RunWithCulture(cultureName, () =>
        {
            var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BalanceMismatch));

            var warning = Assert.Single(file.Report.Warnings);
            Assert.Contains("900.00", warning.Message);
            Assert.Contains("910.00", warning.Message);
        });
    }

    [Fact]
    public void Turkish_dotless_i_does_not_break_tag_recognition()
    {
        TestFixtures.RunWithCulture("tr-TR", () =>
        {
            var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Mt942Intraday));

            var statement = Assert.Single(file.Statements);
            Assert.True(statement.IsIntraday);
            Assert.Equal("NTRF", statement.Lines[0].TransactionType);
        });
    }
}
