using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class SepaStructuredTests
{
    private static readonly Mt940Options Structured = new()
    {
        InformationParser = SlashDelimitedInformationParser.Default,
    };

    private static Mt940Statement ParseSingle(Mt940Options? options = null)
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.SepaStructured), options);
        Assert.False(file.Report.HasWarnings);
        return Assert.Single(file.Statements);
    }

    [Fact]
    public void Default_options_leave_information_raw_and_unstructured()
    {
        var statement = ParseSingle();

        Assert.All(statement.Lines, line => Assert.Empty(line.StructuredInformation));
        Assert.StartsWith("/MARF/MND-2024-0017", statement.Lines[0].Information);
    }

    [Fact]
    public void Raw_information_preserves_continuation_lines()
    {
        var statement = ParseSingle();

        var lines = statement.Lines[0].Information!.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.EndsWith("ENERGIE NED", lines[0]);
        Assert.StartsWith("ERLAND NV", lines[1]);
        Assert.Equal("90123", lines[2]);
    }

    [Fact]
    public void Structured_parser_reads_sepa_sub_fields_across_wrapped_lines()
    {
        var statement = ParseSingle(Structured);

        var info = statement.Lines[0].StructuredInformation;
        Assert.Equal("MND-2024-0017", info["MARF"]);
        Assert.Equal("E2E-2026-08-0042", info["EREF"]);
        Assert.Equal(string.Empty, info["BENM"]);
        Assert.Equal("ENERGIE NEDERLAND NV", info["NAME"]);
        Assert.Equal("CONTRACT 778812 PERIOD JULY 2026", info["REMI"]);
        Assert.Equal("NL13TEST0567890123", info["IBAN"]);
    }

    [Fact]
    public void Structured_parser_reads_the_second_line_independently()
    {
        var statement = ParseSingle(Structured);

        var info = statement.Lines[1].StructuredInformation;
        Assert.Equal("INV-2026-0101", info["EREF"]);
        Assert.Equal(string.Empty, info["ORDP"]);
        Assert.Equal("GLOBEX EUROPE BV", info["NAME"]);
        Assert.Equal("PAYMENT INVOICE 2026-0101", info["REMI"]);
        Assert.Equal("NL69XXXX0333333333", info["IBAN"]);
    }

    [Fact]
    public void Structured_parsing_does_not_change_the_raw_text_or_the_amounts()
    {
        var statement = ParseSingle(Structured);

        Assert.Equal(-850.45m, statement.Lines[0].SignedAmount);
        Assert.Equal(4200.00m, statement.Lines[1].SignedAmount);
        Assert.StartsWith("/MARF/MND-2024-0017", statement.Lines[0].Information);
        Assert.Equal(15349.55m, statement.ClosingBalance!.Amount);
    }
}
