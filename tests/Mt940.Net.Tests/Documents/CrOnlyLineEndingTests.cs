using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class CrOnlyLineEndingTests
{
    [Fact]
    public void Cr_only_files_parse_identically_to_their_lf_twin()
    {
        var crOnlyText = TestFixtures.ReadText(TestFixtures.CrOnly);
        Assert.Contains('\r', crOnlyText);
        Assert.DoesNotContain('\n', crOnlyText);
        var lfTwin = crOnlyText.Replace('\r', '\n');

        var fromCr = Mt940Parser.Parse(crOnlyText);
        var fromLf = Mt940Parser.Parse(lfTwin);

        var crStatement = Assert.Single(fromCr.Statements);
        var lfStatement = Assert.Single(fromLf.Statements);
        Assert.Equal(lfStatement.TransactionReference, crStatement.TransactionReference);
        Assert.Equal(lfStatement.Account, crStatement.Account);
        Assert.Equal(lfStatement.Lines.Count, crStatement.Lines.Count);
        Assert.Equal(lfStatement.Lines[0].SignedAmount, crStatement.Lines[0].SignedAmount);
        Assert.Equal(lfStatement.ClosingBalance!.Amount, crStatement.ClosingBalance!.Amount);
    }

    [Fact]
    public void Cr_only_files_produce_full_statements_not_a_collapsed_20()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.CrOnly));

        Assert.False(file.Report.HasWarnings);
        var statement = Assert.Single(file.Statements);
        Assert.Equal("CR", statement.TransactionReference);
        Assert.Equal("ACC", statement.Account);
        Assert.Equal(-5.00m, Assert.Single(statement.Lines).SignedAmount);
        Assert.Equal(10.00m, statement.OpeningBalance!.Amount);
        Assert.Equal(5.00m, statement.ClosingBalance!.Amount);
        Assert.False(statement.IsIntraday);
    }
}
