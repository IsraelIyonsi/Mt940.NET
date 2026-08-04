using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class MultiStatementTests
{
    private static Mt940File ParseFile()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.MultiStatement));
        Assert.False(file.Report.HasWarnings);
        return file;
    }

    [Fact]
    public void Splits_the_file_into_three_statements_in_order()
    {
        var file = ParseFile();

        Assert.Equal(3, file.Statements.Count);
        Assert.Equal("AUG26SEQ0001", file.Statements[0].TransactionReference);
        Assert.Equal("AUG26SEQ0002", file.Statements[1].TransactionReference);
        Assert.Equal("USD26SEPT0003", file.Statements[2].TransactionReference);
    }

    [Fact]
    public void First_statement_ends_with_an_intermediate_closing_balance()
    {
        var statement = ParseFile().Statements[0];

        Assert.Equal(2, statement.Lines.Count);
        Assert.Equal(-1200.00m, statement.Lines[0].SignedAmount);
        Assert.Equal(350.25m, statement.Lines[1].SignedAmount);
        Assert.NotNull(statement.ClosingBalance);
        Assert.True(statement.ClosingBalance.IsIntermediate);
        Assert.Equal(4150.25m, statement.ClosingBalance.Amount);
        Assert.Equal("GBP", statement.ClosingBalance.Currency);
    }

    [Fact]
    public void Second_statement_continues_with_an_intermediate_opening_balance()
    {
        var statement = ParseFile().Statements[1];

        Assert.Equal("AUG26SEQ0001", statement.RelatedReference);
        Assert.Equal("00212", statement.StatementNumber);
        Assert.Equal("002", statement.SequenceNumber);
        Assert.NotNull(statement.OpeningBalance);
        Assert.True(statement.OpeningBalance.IsIntermediate);
        Assert.Equal(4150.25m, statement.OpeningBalance.Amount);
        Assert.NotNull(statement.ClosingBalance);
        Assert.False(statement.ClosingBalance.IsIntermediate);
        Assert.Equal(4100.50m, statement.ClosingBalance.Amount);
    }

    [Fact]
    public void Third_statement_carries_a_debit_balance_and_no_lines()
    {
        var statement = ParseFile().Statements[2];

        Assert.Empty(statement.Lines);
        Assert.Equal("00090", statement.StatementNumber);
        Assert.Equal("001", statement.SequenceNumber);
        Assert.NotNull(statement.OpeningBalance);
        Assert.Equal(DebitCreditMark.Debit, statement.OpeningBalance.Mark);
        Assert.Equal("USD", statement.OpeningBalance.Currency);
        Assert.Equal(-250.00m, statement.OpeningBalance.SignedAmount);
        Assert.Equal(-250.00m, statement.ClosingBalance!.SignedAmount);
        Assert.False(statement.IsIntraday);
    }
}
