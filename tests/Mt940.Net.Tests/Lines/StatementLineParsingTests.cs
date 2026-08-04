namespace Mt940.Tests.Lines;

public sealed class StatementLineParsingTests
{
    private static StatementLine ParseLine(string line61)
    {
        var text = $"""
            :20:LINETEST
            :25:1234567890
            :28C:00001
            :61:{line61}
            """;
        var file = Mt940Parser.Parse(text);
        return Assert.Single(Assert.Single(file.Statements).Lines);
    }

    [Theory]
    [InlineData("2607020702D250,00NTRFNONREF//BANKREF1", "NONREF", "BANKREF1")]
    [InlineData("2607020702D250,00NTRFCUSTREF", "CUSTREF", null)]
    [InlineData("2607020702D250,00NTRF//ONLYBANK", "", "ONLYBANK")]
    [InlineData("2607020702D250,00NTRFABC-123//X", "ABC-123", "X")]
    public void References_split_on_the_double_slash(string line61, string customer, string? bank)
    {
        var line = ParseLine(line61);

        Assert.Equal(customer, line.CustomerReference);
        Assert.Equal(bank, line.BankReference);
    }

    [Theory]
    [InlineData("2607020702C1,50NTRFR", DebitCreditMark.Credit, null, 1.50)]
    [InlineData("2607020702D2,25NTRFR", DebitCreditMark.Debit, null, -2.25)]
    [InlineData("2607020702RC3,00NTRFR", DebitCreditMark.ReversalCredit, null, -3.00)]
    [InlineData("2607020702RD4,75NTRFR", DebitCreditMark.ReversalDebit, null, 4.75)]
    [InlineData("2607020702CR5,00NTRFR", DebitCreditMark.Credit, 'R', 5.00)]
    [InlineData("2607020702DR6,00NTRFR", DebitCreditMark.Debit, 'R', -6.00)]
    [InlineData("2607020702RDP7,00NTRFR", DebitCreditMark.ReversalDebit, 'P', 7.00)]
    public void Marks_and_funds_codes_disambiguate(
        string line61, DebitCreditMark mark, char? fundsCode, decimal signedAmount)
    {
        var line = ParseLine(line61);

        Assert.Equal(mark, line.Mark);
        Assert.Equal(fundsCode, line.FundsCode);
        Assert.Equal(signedAmount, line.SignedAmount);
    }

    [Theory]
    [InlineData("2607020702D250,00NTRFREF", "NTRF")]
    [InlineData("2607020702D250,00S103REF", "S103")]
    [InlineData("2607020702D250,00F014REF", "F014")]
    [InlineData("2607020702D250,00NMSCREF", "NMSC")]
    public void Transaction_type_codes_parse(string line61, string transactionType)
    {
        Assert.Equal(transactionType, ParseLine(line61).TransactionType);
    }

    [Fact]
    public void Entry_date_is_optional()
    {
        var line = ParseLine("260702D250,00NTRFREF");

        Assert.Equal(new DateOnly(2026, 7, 2), line.ValueDate);
        Assert.Null(line.EntryDate);
    }

    [Fact]
    public void Supplementary_details_come_from_the_continuation_line()
    {
        var text = """
            :20:LINETEST
            :25:1234567890
            :28C:00001
            :61:2607020702D250,00NTRFREF//BNK
            ADDITIONAL SUPPLEMENTARY DETAILS
            """;

        var line = Assert.Single(Assert.Single(Mt940Parser.Parse(text).Statements).Lines);

        Assert.Equal("ADDITIONAL SUPPLEMENTARY DETAILS", line.SupplementaryDetails);
        Assert.Equal("BNK", line.BankReference);
    }

    [Fact]
    public void Customer_reference_longer_than_sixteen_characters_warns_but_parses()
    {
        var text = """
            :20:LINETEST
            :25:1234567890
            :28C:00001
            :61:2607020702D250,00NTRFAVERYLONGREFERENCE99
            """;

        var file = Mt940Parser.Parse(text);

        var line = Assert.Single(Assert.Single(file.Statements).Lines);
        Assert.Equal("AVERYLONGREFERENCE99", line.CustomerReference);
        Assert.Contains(file.Report.Warnings, warning => warning.Tag == "61");
    }

    [Theory]
    [InlineData("GARBAGE")]
    [InlineData("2607020702X250,00NTRFREF")]
    [InlineData("2607020702D250,0,0NTRFREF")]
    [InlineData("2613400702D250,00NTRFREF")]
    [InlineData("2607020702D250,00XTRFREF")]
    [InlineData("2607020702D25O,00NTRFREF")]
    [InlineData("")]
    public void Malformed_lines_throw_with_tag_context(string line61)
    {
        var text = $"""
            :20:LINETEST
            :25:1234567890
            :28C:00001
            :61:{line61}
            """;

        var exception = Assert.Throws<Mt940ParseException>(() => Mt940Parser.Parse(text));

        Assert.Equal("61", exception.Tag);
        Assert.Equal(4, exception.LineNumber);
    }
}
