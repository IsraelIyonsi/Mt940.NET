using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class MinimalStatementTests
{
    private static Mt940Statement ParseSingle()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Minimal));
        Assert.False(file.Report.HasWarnings);
        return Assert.Single(file.Statements);
    }

    [Fact]
    public void Parses_header_fields()
    {
        var statement = ParseSingle();

        Assert.Equal("MINIMAL0001", statement.TransactionReference);
        Assert.Null(statement.RelatedReference);
        Assert.Equal("NL91ABNA0417164300", statement.Account);
        Assert.Equal("00001", statement.StatementNumber);
        Assert.Equal("001", statement.SequenceNumber);
        Assert.False(statement.IsIntraday);
    }

    [Fact]
    public void Parses_opening_balance()
    {
        var statement = ParseSingle();

        var opening = statement.OpeningBalance;
        Assert.NotNull(opening);
        Assert.Equal(DebitCreditMark.Credit, opening.Mark);
        Assert.Equal(new DateOnly(2026, 7, 1), opening.Date);
        Assert.Equal("EUR", opening.Currency);
        Assert.Equal(1000.00m, opening.Amount);
        Assert.Equal(1000.00m, opening.SignedAmount);
        Assert.False(opening.IsIntermediate);
    }

    [Fact]
    public void Parses_the_statement_line_completely()
    {
        var statement = ParseSingle();

        var line = Assert.Single(statement.Lines);
        Assert.Equal(new DateOnly(2026, 7, 2), line.ValueDate);
        Assert.Equal(new DateOnly(2026, 7, 2), line.EntryDate);
        Assert.Equal(DebitCreditMark.Debit, line.Mark);
        Assert.Null(line.FundsCode);
        Assert.Equal(250.00m, line.Amount);
        Assert.Equal(-250.00m, line.SignedAmount);
        Assert.Equal("NTRF", line.TransactionType);
        Assert.Equal("NONREF", line.CustomerReference);
        Assert.Equal("B4E07C58Q9", line.BankReference);
        Assert.Equal("INTERNET TRANSFER TO SAVINGS ACCOUNT", line.SupplementaryDetails);
        Assert.Equal("TRANSFER TO SAVINGS", line.Information);
        Assert.Empty(line.StructuredInformation);
    }

    [Fact]
    public void Parses_closing_and_available_balances()
    {
        var statement = ParseSingle();

        Assert.NotNull(statement.ClosingBalance);
        Assert.Equal(DebitCreditMark.Credit, statement.ClosingBalance.Mark);
        Assert.Equal(new DateOnly(2026, 7, 2), statement.ClosingBalance.Date);
        Assert.Equal("EUR", statement.ClosingBalance.Currency);
        Assert.Equal(750.00m, statement.ClosingBalance.Amount);
        Assert.False(statement.ClosingBalance.IsIntermediate);

        Assert.NotNull(statement.ClosingAvailableBalance);
        Assert.Equal(750.00m, statement.ClosingAvailableBalance.Amount);
    }

    [Fact]
    public void Parses_repeated_forward_available_balances_in_order()
    {
        var statement = ParseSingle();

        Assert.Equal(2, statement.ForwardAvailableBalances.Count);
        Assert.Equal(new DateOnly(2026, 7, 3), statement.ForwardAvailableBalances[0].Date);
        Assert.Equal(new DateOnly(2026, 7, 4), statement.ForwardAvailableBalances[1].Date);
        Assert.All(statement.ForwardAvailableBalances, balance => Assert.Equal(750.00m, balance.Amount));
    }

    [Fact]
    public void Exposes_debit_and_credit_totals()
    {
        var statement = ParseSingle();

        Assert.Equal(250.00m, statement.TotalDebits);
        Assert.Equal(0m, statement.TotalCredits);
    }

    [Fact]
    public void Has_no_mt942_fields()
    {
        var statement = ParseSingle();

        Assert.Null(statement.ReportDateTime);
        Assert.Null(statement.DebitFloorLimit);
        Assert.Null(statement.CreditFloorLimit);
        Assert.Null(statement.DebitEntrySummary);
        Assert.Null(statement.CreditEntrySummary);
        Assert.Empty(statement.UnknownTags);
        Assert.Null(statement.InformationToAccountOwner);
    }

    [Fact]
    public void Parses_identically_with_crlf_line_endings()
    {
        var text = TestFixtures.ReadText(TestFixtures.Minimal).Replace("\n", "\r\n");

        var file = Mt940Parser.Parse(text);

        var statement = Assert.Single(file.Statements);
        Assert.False(file.Report.HasWarnings);
        Assert.Equal("MINIMAL0001", statement.TransactionReference);
        Assert.Equal("TRANSFER TO SAVINGS", Assert.Single(statement.Lines).Information);
        Assert.Equal(750.00m, statement.ClosingBalance!.Amount);
    }
}
