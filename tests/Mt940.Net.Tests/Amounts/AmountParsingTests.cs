using Mt940.Internal;

namespace Mt940.Tests.Amounts;

public sealed class AmountParsingTests
{
    [Theory]
    [InlineData("1234,56", "1234.56")]
    [InlineData("1234,", "1234")]
    [InlineData("1234", "1234")]
    [InlineData(",50", "0.50")]
    [InlineData("0,", "0")]
    [InlineData("0,01", "0.01")]
    [InlineData("999999999999,99", "999999999999.99")]
    [InlineData("123456789,1", "123456789.1")]
    [InlineData("000123,40", "123.40")]
    public void Comma_decimal_amounts_parse_exactly(string raw, string expected)
    {
        var amount = FieldParsers.ParseAmount(raw, lineNumber: 1, tag: "61");

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), amount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(",")]
    [InlineData("12,3,4")]
    [InlineData("12.34")]
    [InlineData("1 234,00")]
    [InlineData("-12,00")]
    [InlineData("12A")]
    [InlineData("123456789012345678901234567890,")]
    public void Invalid_amounts_throw_a_parse_exception(string raw)
    {
        var exception = Assert.Throws<Mt940ParseException>(
            () => FieldParsers.ParseAmount(raw, lineNumber: 7, tag: "60F"));

        Assert.Equal(7, exception.LineNumber);
        Assert.Equal("60F", exception.Tag);
    }

    [Fact]
    public void Amounts_never_lose_precision_to_binary_floating_point()
    {
        var amount = FieldParsers.ParseAmount("0,10", lineNumber: 1, tag: "61");

        Assert.Equal(0.10m, amount);
        Assert.Equal(0.30m, amount + FieldParsers.ParseAmount("0,20", lineNumber: 1, tag: "61"));
    }
}
