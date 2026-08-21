namespace Mt940.Tests.Information;

public sealed class GermanGvcParserTests
{
    private static readonly GermanGvcInformationParser Parser = GermanGvcInformationParser.Default;

    private const string RealisticInformation =
        "166?00SEPA-UEBERWEISUNG?20EREF+INV-2024-0042?21SVWZ+Rechnung 42?22 Miete Mai" +
        "?30GENODEF1S02?31DE02500105170137075030?32Max Mustermann?33GmbH?34999";

    [Fact]
    public void Parses_a_realistic_field_86_value()
    {
        var result = Parser.Parse(RealisticInformation);

        Assert.Equal("166", result[GermanGvcInformationParser.GvcKey]);
        Assert.Equal("SEPA-UEBERWEISUNG", result[GermanGvcInformationParser.BookingTextKey]);
        Assert.Equal(
            "EREF+INV-2024-0042SVWZ+Rechnung 42 Miete Mai",
            result[GermanGvcInformationParser.PurposeKey]);
        Assert.Equal("GENODEF1S02", result[GermanGvcInformationParser.BicKey]);
        Assert.Equal("DE02500105170137075030", result[GermanGvcInformationParser.IbanKey]);
        Assert.Equal("Max MustermannGmbH", result[GermanGvcInformationParser.NameKey]);
        Assert.Equal("999", result[GermanGvcInformationParser.TextKeyKey]);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void Purpose_concatenates_the_parts_in_the_order_they_appear()
    {
        var result = Parser.Parse("020?20FIRST ?21SECOND ?60SIXTY ?63SIXTYTHREE");

        Assert.Equal(
            "FIRST SECOND SIXTY SIXTYTHREE",
            result[GermanGvcInformationParser.PurposeKey]);
    }

    [Fact]
    public void A_value_with_only_a_gvc_and_no_sub_fields_yields_just_the_gvc()
    {
        var result = Parser.Parse("166");

        Assert.Single(result);
        Assert.Equal("166", result[GermanGvcInformationParser.GvcKey]);
    }

    [Fact]
    public void Name_concatenates_both_parts_without_a_separator()
    {
        var result = Parser.Parse("051?32Max Mustermann?33GmbH & Co KG");

        Assert.Equal("Max MustermannGmbH & Co KG", result[GermanGvcInformationParser.NameKey]);
    }

    [Fact]
    public void Unrecognized_codes_are_preserved_under_their_raw_marker_key()
    {
        var result = Parser.Parse("166?40BIC-ANGABE?00TEXT");

        Assert.Equal("BIC-ANGABE", result["?40"]);
        Assert.Equal("TEXT", result[GermanGvcInformationParser.BookingTextKey]);
    }

    [Fact]
    public void Line_wrapping_inside_a_token_is_repaired_before_scanning()
    {
        var result = Parser.Parse("166?00SEPA-UEBER\nWEISUNG?31DE0250010\r\n5170137075030");

        Assert.Equal("SEPA-UEBERWEISUNG", result[GermanGvcInformationParser.BookingTextKey]);
        Assert.Equal("DE02500105170137075030", result[GermanGvcInformationParser.IbanKey]);
    }

    [Fact]
    public void Malformed_single_digit_marker_is_kept_as_content_not_a_sub_field()
    {
        var result = Parser.Parse("166?00SEPA?3?20ZWECK");

        Assert.Equal("SEPA?3", result[GermanGvcInformationParser.BookingTextKey]);
        Assert.Equal("ZWECK", result[GermanGvcInformationParser.PurposeKey]);
        Assert.False(result.ContainsKey("?3"));
    }

    [Fact]
    public void A_value_beginning_directly_with_a_sub_field_has_no_gvc()
    {
        var result = Parser.Parse("?00SEPA-UEBERWEISUNG");

        Assert.False(result.ContainsKey(GermanGvcInformationParser.GvcKey));
        Assert.Equal("SEPA-UEBERWEISUNG", result[GermanGvcInformationParser.BookingTextKey]);
    }

    [Fact]
    public void Repeated_single_value_sub_fields_keep_the_last_occurrence()
    {
        var result = Parser.Parse("166?00FIRST?00SECOND");

        Assert.Equal("SECOND", result[GermanGvcInformationParser.BookingTextKey]);
    }

    [Fact]
    public void Empty_input_yields_nothing()
    {
        Assert.Empty(Parser.Parse(string.Empty));
    }

    [Fact]
    public void Parse_rejects_null_information()
    {
        Assert.Throws<ArgumentNullException>(() => Parser.Parse(null!));
    }
}
