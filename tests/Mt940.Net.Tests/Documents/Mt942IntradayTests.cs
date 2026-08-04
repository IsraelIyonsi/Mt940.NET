using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class Mt942IntradayTests
{
    private static Mt940Statement ParseSingle()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Mt942Intraday));
        Assert.False(file.Report.HasWarnings);
        return Assert.Single(file.Statements);
    }

    [Fact]
    public void Detects_an_intraday_report()
    {
        var statement = ParseSingle();

        Assert.True(statement.IsIntraday);
        Assert.Null(statement.OpeningBalance);
        Assert.Null(statement.ClosingBalance);
        Assert.Equal("INTRA260721X01", statement.TransactionReference);
        Assert.Equal("NONREF", statement.RelatedReference);
        Assert.Equal("DE89370400440532013000", statement.Account);
        Assert.Equal("00123", statement.StatementNumber);
        Assert.Equal("001", statement.SequenceNumber);
    }

    [Fact]
    public void Parses_the_date_time_indication_with_offset()
    {
        var statement = ParseSingle();

        Assert.Equal(
            new DateTimeOffset(2026, 7, 21, 13, 30, 0, TimeSpan.FromHours(2)),
            statement.ReportDateTime);
    }

    [Fact]
    public void Parses_debit_and_credit_floor_limits()
    {
        var statement = ParseSingle();

        Assert.NotNull(statement.DebitFloorLimit);
        Assert.Equal("EUR", statement.DebitFloorLimit.Currency);
        Assert.Equal(5m, statement.DebitFloorLimit.Amount);
        Assert.NotNull(statement.CreditFloorLimit);
        Assert.Equal("EUR", statement.CreditFloorLimit.Currency);
        Assert.Equal(5m, statement.CreditFloorLimit.Amount);
    }

    [Fact]
    public void A_single_unmarked_floor_limit_applies_to_both_sides()
    {
        const string text = """
            :20:INTRA0002
            :25:1234567890
            :28C:00124/001
            :34F:USD250,
            :13D:2607211500+0000
            """;

        var statement = Assert.Single(Mt940Parser.Parse(text).Statements);

        Assert.NotNull(statement.DebitFloorLimit);
        Assert.NotNull(statement.CreditFloorLimit);
        Assert.Equal(250m, statement.DebitFloorLimit.Amount);
        Assert.Equal(250m, statement.CreditFloorLimit.Amount);
        Assert.Equal("USD", statement.CreditFloorLimit.Currency);
    }

    [Fact]
    public void Parses_entry_summaries()
    {
        var statement = ParseSingle();

        Assert.Equal(new EntrySummary(1, "EUR", 200.50m), statement.DebitEntrySummary);
        Assert.Equal(new EntrySummary(1, "EUR", 1500.00m), statement.CreditEntrySummary);
    }

    [Fact]
    public void Entry_summaries_agree_with_the_line_totals()
    {
        var statement = ParseSingle();

        Assert.Equal(statement.DebitEntrySummary!.Amount, statement.TotalDebits);
        Assert.Equal(statement.CreditEntrySummary!.Amount, statement.TotalCredits);
    }

    [Fact]
    public void Parses_intraday_lines_like_statement_lines()
    {
        var statement = ParseSingle();

        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(1500.00m, statement.Lines[0].SignedAmount);
        Assert.Equal("E2E-42", statement.Lines[0].CustomerReference);
        Assert.Equal("BK55X1", statement.Lines[0].BankReference);
        Assert.Equal(-200.50m, statement.Lines[1].SignedAmount);
        Assert.Equal("NDDT", statement.Lines[1].TransactionType);
    }
}
