using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class UnknownTagTests
{
    private static Mt940File ParseFile() =>
        Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.UnknownTags));

    [Fact]
    public void Unknown_tags_are_kept_raw_in_file_order()
    {
        var statement = Assert.Single(ParseFile().Statements);

        Assert.Equal(2, statement.UnknownTags.Count);
        Assert.Equal("NS", statement.UnknownTags[0].Tag);
        Assert.Equal("22INTERNAL BANK DATA", statement.UnknownTags[0].Value);
        Assert.Equal(4, statement.UnknownTags[0].LineNumber);
        Assert.Equal("23E", statement.UnknownTags[1].Tag);
        Assert.Equal("PROPRIETARY EXTENSION", statement.UnknownTags[1].Value);
        Assert.Equal(8, statement.UnknownTags[1].LineNumber);
    }

    [Fact]
    public void Unknown_tags_raise_warnings_but_never_fail_the_parse()
    {
        var file = ParseFile();

        Assert.Equal(2, file.Report.Warnings.Count);
        Assert.All(file.Report.Warnings, warning => Assert.Equal(0, warning.StatementIndex));
        Assert.Equal("NS", file.Report.Warnings[0].Tag);
        Assert.Equal("23E", file.Report.Warnings[1].Tag);
    }

    [Fact]
    public void Known_fields_around_unknown_tags_parse_normally()
    {
        var statement = Assert.Single(ParseFile().Statements);

        Assert.Equal(400.00m, statement.OpeningBalance!.Amount);
        Assert.Equal(500.00m, statement.ClosingBalance!.Amount);
        var line = Assert.Single(statement.Lines);
        Assert.Equal(100.00m, line.Amount);
        Assert.Equal("CREDIT TRANSFER", line.Information);
    }
}
