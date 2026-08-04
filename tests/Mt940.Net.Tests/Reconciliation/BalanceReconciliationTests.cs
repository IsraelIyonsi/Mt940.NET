using Mt940.Tests.Support;

namespace Mt940.Tests.Reconciliation;

public sealed class BalanceReconciliationTests
{
    public static TheoryData<string> ReconcilableFixtures()
    {
        var data = new TheoryData<string>();
        foreach (var fixture in TestFixtures.Reconcilable)
        {
            data.Add(fixture);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ReconcilableFixtures))]
    public void Opening_plus_signed_lines_equals_closing_on_every_statement(string fixture)
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(fixture));

        Assert.NotEmpty(file.Statements);
        foreach (var statement in file.Statements)
        {
            Assert.NotNull(statement.OpeningBalance);
            Assert.NotNull(statement.ClosingBalance);
            var sumOfLines = statement.Lines.Sum(line => line.SignedAmount);
            Assert.Equal(
                statement.ClosingBalance.SignedAmount,
                statement.OpeningBalance.SignedAmount + sumOfLines);
        }
    }

    [Theory]
    [MemberData(nameof(ReconcilableFixtures))]
    public void Total_credits_minus_total_debits_equals_the_balance_movement(string fixture)
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(fixture));

        foreach (var statement in file.Statements)
        {
            Assert.Equal(
                statement.ClosingBalance!.SignedAmount - statement.OpeningBalance!.SignedAmount,
                statement.TotalCredits - statement.TotalDebits);
            Assert.Equal(
                statement.Lines.Sum(line => line.SignedAmount),
                statement.TotalCredits - statement.TotalDebits);
        }
    }

    [Fact]
    public void Mismatch_warns_by_default_with_expected_and_actual_amounts()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BalanceMismatch));

        Assert.Single(file.Statements);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal(0, warning.StatementIndex);
        Assert.Equal("62F", warning.Tag);
        Assert.Contains("900.00", warning.Message);
        Assert.Contains("910.00", warning.Message);
    }

    [Fact]
    public void Mismatch_throws_when_configured_to_throw()
    {
        var options = new Mt940Options { BalanceMismatchBehavior = BalanceMismatchBehavior.Throw };

        var exception = Assert.Throws<Mt940ParseException>(
            () => Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BalanceMismatch), options));

        Assert.Equal("62F", exception.Tag);
        Assert.Equal(7, exception.LineNumber);
        Assert.Contains("900.00", exception.Message);
    }

    [Fact]
    public void Reconciling_statements_do_not_warn_even_when_configured_to_throw()
    {
        var options = new Mt940Options { BalanceMismatchBehavior = BalanceMismatchBehavior.Throw };

        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Minimal), options);

        Assert.False(file.Report.HasWarnings);
        Assert.Single(file.Statements);
    }

    [Fact]
    public void Intraday_reports_without_balances_are_not_reconciled()
    {
        var options = new Mt940Options { BalanceMismatchBehavior = BalanceMismatchBehavior.Throw };

        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Mt942Intraday), options);

        Assert.False(file.Report.HasWarnings);
    }
}
