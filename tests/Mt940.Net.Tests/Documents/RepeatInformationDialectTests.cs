using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

/// <summary>
/// ING (and some other banks) emit several consecutive :86: fields for one :61: line instead of
/// continuation lines. Those must stay on the line, not leak into statement-level information.
/// </summary>
public sealed class RepeatInformationDialectTests
{
    private static Mt940File ParseFile(Mt940Options? options = null) =>
        Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.IngRepeat86), options);

    [Fact]
    public void Consecutive_86_fields_append_to_the_line_information()
    {
        var statement = Assert.Single(ParseFile().Statements);

        var line = Assert.Single(statement.Lines);
        Assert.Equal(
            "/EREF/E2E-42/\n/ORDP//NAME/ACME BV/\n/REMI/USTD//INVOICE 42/",
            line.Information);
    }

    [Fact]
    public void Nothing_leaks_into_statement_level_information()
    {
        var statement = Assert.Single(ParseFile().Statements);

        Assert.Null(statement.InformationToAccountOwner);
    }

    [Fact]
    public void The_repeat_dialect_is_flagged_with_exactly_one_warning()
    {
        var file = ParseFile();

        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("86", warning.Tag);
        Assert.Equal(0, warning.StatementIndex);
        Assert.Contains(":86:", warning.Message);
    }

    [Fact]
    public void The_structured_parser_sees_the_appended_information()
    {
        var options = new Mt940Options { InformationParser = SlashDelimitedInformationParser.Default };

        var line = Assert.Single(Assert.Single(ParseFile(options).Statements).Lines);

        Assert.Equal("E2E-42/", line.StructuredInformation["EREF"]);
        Assert.Equal(string.Empty, line.StructuredInformation["ORDP"]);
        Assert.Equal("ACME BV/", line.StructuredInformation["NAME"]);
        Assert.Equal("USTD//INVOICE 42/", line.StructuredInformation["REMI"]);
    }

    [Fact]
    public void The_line_itself_is_unaffected()
    {
        var statement = Assert.Single(ParseFile().Statements);

        var line = Assert.Single(statement.Lines);
        Assert.Equal(500.00m, line.SignedAmount);
        Assert.Equal("EREF", line.CustomerReference);
        Assert.Equal("INGA00000XXXXX", line.BankReference);
        Assert.Equal("/TRCD/00100/", line.SupplementaryDetails);
        Assert.Equal(1500.00m, statement.ClosingBalance!.Amount);
    }

    [Fact]
    public void An_86_after_the_closing_balance_is_still_statement_level()
    {
        const string text = """
            :20:AFTERCLOSE
            :25:1234567890
            :28C:1
            :60F:C260101EUR100,00
            :61:2601020102D40,00NTRFNONREF
            :86:LINE NOTE
            :62F:C260102EUR60,00
            :86:STATEMENT NOTE
            """;

        var statement = Assert.Single(Mt940Parser.Parse(text).Statements);

        Assert.Equal("LINE NOTE", Assert.Single(statement.Lines).Information);
        Assert.Equal("STATEMENT NOTE", statement.InformationToAccountOwner);
    }
}
