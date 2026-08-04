namespace Mt940.Tests.Information;

public sealed class SlashDelimitedParserTests
{
    private static readonly SlashDelimitedInformationParser Parser = SlashDelimitedInformationParser.Default;

    [Fact]
    public void Parses_a_sequence_of_sub_fields()
    {
        var result = Parser.Parse("/EREF/E2E-42/MARF/MND-001/REMI/INVOICE 42");

        Assert.Equal(3, result.Count);
        Assert.Equal("E2E-42", result["EREF"]);
        Assert.Equal("MND-001", result["MARF"]);
        Assert.Equal("INVOICE 42", result["REMI"]);
    }

    [Fact]
    public void Nested_counterparty_tags_yield_an_empty_group_value()
    {
        var result = Parser.Parse("/BENM//NAME/ACME BV/IBAN/NL91ABNA0417164300");

        Assert.Equal(string.Empty, result["BENM"]);
        Assert.Equal("ACME BV", result["NAME"]);
        Assert.Equal("NL91ABNA0417164300", result["IBAN"]);
    }

    [Fact]
    public void Values_may_contain_single_slashes()
    {
        var result = Parser.Parse("/REMI/A/B TEST 50/50 SPLIT/EREF/X1");

        Assert.Equal("A/B TEST 50/50 SPLIT", result["REMI"]);
        Assert.Equal("X1", result["EREF"]);
    }

    [Fact]
    public void Line_wrapping_inside_a_token_is_repaired_before_scanning()
    {
        var result = Parser.Parse("/ERE\nF/E2E-42/REM\r\nI/HELLO");

        Assert.Equal("E2E-42", result["EREF"]);
        Assert.Equal("HELLO", result["REMI"]);
    }

    [Fact]
    public void Text_without_recognized_sub_tags_yields_nothing()
    {
        Assert.Empty(Parser.Parse("FREE TEXT DESCRIPTION 1/2"));
        Assert.Empty(Parser.Parse(string.Empty));
    }

    [Fact]
    public void Leading_free_text_before_the_first_sub_tag_is_ignored()
    {
        var result = Parser.Parse("SEPA OVERBOEKING /EREF/X9");

        Assert.Single(result);
        Assert.Equal("X9", result["EREF"]);
    }

    [Fact]
    public void Repeated_sub_tags_keep_the_last_occurrence()
    {
        var result = Parser.Parse("/EREF/FIRST/EREF/SECOND");

        Assert.Equal("SECOND", result["EREF"]);
    }

    [Fact]
    public void Custom_sub_tag_sets_replace_the_default()
    {
        var parser = new SlashDelimitedInformationParser(["TRTP", "XXCUSTOM"]);

        var result = parser.Parse("/TRTP/SEPA DD/XXCUSTOM/1/EREF/IGNORED-TAG");

        Assert.Equal("SEPA DD", result["TRTP"]);
        Assert.Equal("1/EREF/IGNORED-TAG", result["XXCUSTOM"]);
        Assert.False(result.ContainsKey("EREF"));
    }

    [Fact]
    public void Constructor_validates_the_sub_tag_set()
    {
        Assert.Throws<ArgumentNullException>(() => new SlashDelimitedInformationParser(null!));
        Assert.Throws<ArgumentException>(() => new SlashDelimitedInformationParser(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => new SlashDelimitedInformationParser(["A/B"]));
        Assert.Throws<ArgumentException>(() => new SlashDelimitedInformationParser([""]));
    }

    [Fact]
    public void Parse_rejects_null_information()
    {
        Assert.Throws<ArgumentNullException>(() => Parser.Parse(null!));
    }

    [Fact]
    public void Default_sub_tags_cover_the_common_sepa_code_words()
    {
        Assert.Contains("EREF", SlashDelimitedInformationParser.DefaultSubTags);
        Assert.Contains("MARF", SlashDelimitedInformationParser.DefaultSubTags);
        Assert.Contains("BENM", SlashDelimitedInformationParser.DefaultSubTags);
        Assert.Contains("REMI", SlashDelimitedInformationParser.DefaultSubTags);
        Assert.Contains("IBAN", SlashDelimitedInformationParser.DefaultSubTags);
    }
}
