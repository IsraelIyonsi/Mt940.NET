using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class ReversalMarkTests
{
    private static Mt940Statement ParseSingle()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.ReversalMarks));
        Assert.False(file.Report.HasWarnings);
        return Assert.Single(file.Statements);
    }

    [Fact]
    public void Reversal_of_debit_is_credit_directed()
    {
        var line = ParseSingle().Lines[0];

        Assert.Equal(DebitCreditMark.ReversalDebit, line.Mark);
        Assert.Equal(50.00m, line.Amount);
        Assert.Equal(50.00m, line.SignedAmount);
        Assert.Equal("NONREF", line.CustomerReference);
        Assert.Equal("RTN042", line.BankReference);
    }

    [Fact]
    public void Reversal_of_credit_is_debit_directed()
    {
        var line = ParseSingle().Lines[1];

        Assert.Equal(DebitCreditMark.ReversalCredit, line.Mark);
        Assert.Equal(25.00m, line.Amount);
        Assert.Equal(-25.00m, line.SignedAmount);
        Assert.Equal("REF-889", line.CustomerReference);
        Assert.Null(line.BankReference);
    }

    [Fact]
    public void Credit_mark_followed_by_funds_code_is_not_misread_as_reversal()
    {
        var line = ParseSingle().Lines[2];

        Assert.Equal(DebitCreditMark.Credit, line.Mark);
        Assert.Equal('R', line.FundsCode);
        Assert.Equal(100.00m, line.Amount);
        Assert.Equal(100.00m, line.SignedAmount);
    }

    [Fact]
    public void Reversals_participate_in_balance_reconciliation()
    {
        var statement = ParseSingle();

        Assert.Equal(225.00m, statement.ClosingBalance!.SignedAmount);
        Assert.Equal(25.00m, statement.TotalDebits);
        Assert.Equal(150.00m, statement.TotalCredits);
    }
}
