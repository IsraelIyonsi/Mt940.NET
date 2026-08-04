using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class BlockWrappedTests
{
    [Fact]
    public void Parses_a_message_wrapped_in_swift_blocks()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BlockWrapped));

        Assert.False(file.Report.HasWarnings);
        var statement = Assert.Single(file.Statements);
        Assert.Equal("B4G30MS9D00A0009", statement.TransactionReference);
        Assert.Equal("NL08INGB0000001234EUR", statement.Account);
        Assert.Equal("00135", statement.StatementNumber);
    }

    [Fact]
    public void Parses_the_line_inside_the_block()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BlockWrapped));

        var line = Assert.Single(Assert.Single(file.Statements).Lines);
        Assert.Equal(750.00m, line.Amount);
        Assert.Equal(DebitCreditMark.Credit, line.Mark);
        Assert.Equal("EV1234REP", line.CustomerReference);
        Assert.Equal("INGREF001", line.BankReference);
        Assert.Equal("/EREF/EV1234REP/CNTP/NL91ABNA0417164300/NAME/ACME BV", line.Information);
    }

    [Fact]
    public void Reconciles_and_ignores_header_and_trailer_blocks()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BlockWrapped));

        var statement = Assert.Single(file.Statements);
        Assert.Equal(2500.00m, statement.OpeningBalance!.SignedAmount);
        Assert.Equal(3250.00m, statement.ClosingBalance!.SignedAmount);
        Assert.False(statement.IsIntraday);
    }

    [Fact]
    public void Applies_the_structured_information_parser_inside_blocks()
    {
        var options = new Mt940Options { InformationParser = SlashDelimitedInformationParser.Default };

        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.BlockWrapped), options);

        var line = Assert.Single(Assert.Single(file.Statements).Lines);
        Assert.Equal("EV1234REP", line.StructuredInformation["EREF"]);
        Assert.Equal("NL91ABNA0417164300", line.StructuredInformation["CNTP"]);
        Assert.Equal("ACME BV", line.StructuredInformation["NAME"]);
    }

    [Fact]
    public void Bare_and_block_wrapped_bodies_parse_to_the_same_statement()
    {
        var wrapped = TestFixtures.ReadText(TestFixtures.BlockWrapped);
        var start = wrapped.IndexOf(":20:", StringComparison.Ordinal);
        var end = wrapped.IndexOf("\r\n-}", StringComparison.Ordinal);
        var bare = wrapped[start..end];

        var fromWrapped = Assert.Single(Mt940Parser.Parse(wrapped).Statements);
        var fromBare = Assert.Single(Mt940Parser.Parse(bare).Statements);

        Assert.Equal(fromBare.TransactionReference, fromWrapped.TransactionReference);
        Assert.Equal(fromBare.ClosingBalance!.Amount, fromWrapped.ClosingBalance!.Amount);
        Assert.Equal(fromBare.Lines.Count, fromWrapped.Lines.Count);
    }
}
