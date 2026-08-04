using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class MultilineInformationTests
{
    [Fact]
    public void Information_with_six_continuation_lines_is_preserved_in_order()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Multiline86));

        Assert.False(file.Report.HasWarnings);
        var line = Assert.Single(Assert.Single(file.Statements).Lines);
        var informationLines = line.Information!.Split('\n');
        Assert.Equal(7, informationLines.Length);
        Assert.Equal("LINE ONE OF THE PAYMENT DESCRIPTION GOES HERE", informationLines[0]);
        Assert.Equal("LINE SEVEN IS THE FINAL CONTINUATION", informationLines[6]);
    }

    [Fact]
    public void Continuation_lines_do_not_leak_into_other_fields()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Multiline86));

        var statement = Assert.Single(file.Statements);
        var line = Assert.Single(statement.Lines);
        Assert.Equal(123.45m, line.Amount);
        Assert.Equal("REF778899", line.BankReference);
        Assert.Equal(776.55m, statement.ClosingBalance!.Amount);
    }
}
