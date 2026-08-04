using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class OrphanInformationTests
{
    [Fact]
    public void An_86_in_the_transaction_region_without_a_preceding_61_warns()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.InformationBefore61));

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("86", warning.Tag);
        Assert.Equal(0, warning.StatementIndex);
        Assert.Equal(5, warning.LineNumber);
        Assert.Contains(":61:", warning.Message);
    }

    [Fact]
    public void The_orphan_text_is_kept_as_statement_information_not_dropped()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.InformationBefore61));

        var statement = Assert.Single(file.Statements);
        Assert.Equal("NARRATIVE FOR THE PAYMENT BELOW", statement.InformationToAccountOwner);
        Assert.Null(Assert.Single(statement.Lines).Information);
        Assert.Equal(60.00m, statement.ClosingBalance!.Amount);
    }
}
