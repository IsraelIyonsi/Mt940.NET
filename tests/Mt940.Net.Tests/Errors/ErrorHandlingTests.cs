using Mt940.Tests.Support;

namespace Mt940.Tests.Errors;

public sealed class ErrorHandlingTests
{
    [Fact]
    public void Parse_rejects_null_text()
    {
        Assert.Throws<ArgumentNullException>(() => Mt940Parser.Parse(null!));
    }

    [Fact]
    public async Task ParseAsync_rejects_a_null_stream()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Mt940Parser.ParseAsync(null!));
    }

    [Fact]
    public void Malformed_balance_reports_line_number_and_tag()
    {
        const string text = """
            :20:BROKEN0001
            :25:1234567890
            :28C:00001
            :60F:XINVALIDBALANCE
            """;

        var exception = Assert.Throws<Mt940ParseException>(() => Mt940Parser.Parse(text));

        Assert.Equal(4, exception.LineNumber);
        Assert.Equal("60F", exception.Tag);
        Assert.Contains("line 4", exception.Message);
        Assert.Contains(":60F:", exception.Message);
    }

    [Fact]
    public void Malformed_statement_number_reports_context()
    {
        const string text = """
            :20:BROKEN0002
            :25:1234567890
            :28C:NOTANUMBER
            """;

        var exception = Assert.Throws<Mt940ParseException>(() => Mt940Parser.Parse(text));

        Assert.Equal(3, exception.LineNumber);
        Assert.Equal("28C", exception.Tag);
    }

    [Fact]
    public void Malformed_date_time_indication_reports_context()
    {
        const string text = """
            :20:BROKEN0003
            :25:1234567890
            :28C:00001
            :13D:26072199
            """;

        var exception = Assert.Throws<Mt940ParseException>(() => Mt940Parser.Parse(text));

        Assert.Equal(4, exception.LineNumber);
        Assert.Equal("13D", exception.Tag);
    }

    [Fact]
    public void Parse_exception_is_a_format_exception()
    {
        Assert.IsAssignableFrom<FormatException>(new Mt940ParseException("boom"));
    }

    [Fact]
    public void Parse_exception_without_context_has_no_position()
    {
        var exception = new Mt940ParseException("boom");

        Assert.Equal(0, exception.LineNumber);
        Assert.Null(exception.Tag);
        Assert.Equal("boom", exception.Message);
    }

    [Theory]
    [InlineData(":20:A\n:61:GARBAGE")]
    [InlineData(":20:A\n:60F:XINVALID")]
    [InlineData(":20:A\n:28C:NOPE")]
    public void TryParse_returns_false_instead_of_throwing(string text)
    {
        var parsed = Mt940Parser.TryParse(text, out var file);

        Assert.False(parsed);
        Assert.Null(file);
    }

    [Fact]
    public void TryParse_returns_false_for_null_text()
    {
        var parsed = Mt940Parser.TryParse(null, out var file);

        Assert.False(parsed);
        Assert.Null(file);
    }

    [Fact]
    public void TryParse_respects_the_throw_option_by_returning_false()
    {
        var options = new Mt940Options { BalanceMismatchBehavior = BalanceMismatchBehavior.Throw };

        var parsed = Mt940Parser.TryParse(
            TestFixtures.ReadText(TestFixtures.BalanceMismatch), options, out var file);

        Assert.False(parsed);
        Assert.Null(file);
    }

    [Fact]
    public void Lines_before_the_first_statement_warn_and_are_not_dropped_silently()
    {
        const string text = """
            :25:ORPHAN
            :20:REAL0001
            :25:1234567890
            :28C:00001
            """;

        var file = Mt940Parser.Parse(text);

        Assert.Single(file.Statements);
        var warning = Assert.Single(file.Report.Warnings, w => w.Tag == "25" && w.StatementIndex is null);
        Assert.Equal(1, warning.LineNumber);
    }

    [Theory]
    [InlineData(":20:REF0001\nUNEXPECTED CONTINUATION\n:25:1234567890\n:28C:00001", "20")]
    [InlineData(":20:REF0001\n:25:1234567890\nACCOUNT CONTINUATION\n:28C:00001", "25")]
    [InlineData(":20:REF0001\n:25:1234567890\n:28C:00001\nMORE", "28C")]
    [InlineData(":20:REF0001\n:21:RELREF\nRELATED CONTINUATION\n:25:1234567890\n:28C:00001", "21")]
    public void Continuations_on_single_line_tags_warn_when_truncated(string text, string tag)
    {
        var file = Mt940Parser.Parse(text);

        var statement = Assert.Single(file.Statements);
        Assert.Equal("REF0001", statement.TransactionReference);
        Assert.Contains(
            file.Report.Warnings,
            warning => warning.Tag == tag && warning.Message.Contains("continuation"));
    }

    [Fact]
    public void Duplicate_balance_tags_keep_the_first_and_warn()
    {
        const string text = """
            :20:DUP0001
            :25:1234567890
            :28C:00001
            :60F:C260701EUR100,00
            :60F:C260701EUR999,99
            :62F:C260701EUR100,00
            """;

        var file = Mt940Parser.Parse(text);

        var statement = Assert.Single(file.Statements);
        Assert.Equal(100.00m, statement.OpeningBalance!.Amount);
        Assert.Contains(file.Report.Warnings, warning => warning.Tag == "60F" && warning.LineNumber == 5);
    }
}
