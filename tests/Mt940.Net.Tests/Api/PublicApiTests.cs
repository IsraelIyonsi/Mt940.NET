using Mt940.Tests.Support;

namespace Mt940.Tests.Api;

public sealed class PublicApiTests
{
    [Fact]
    public void TryParse_succeeds_on_a_valid_file()
    {
        var parsed = Mt940Parser.TryParse(TestFixtures.ReadText(TestFixtures.Minimal), out var file);

        Assert.True(parsed);
        Assert.NotNull(file);
        Assert.Single(file.Statements);
    }

    [Fact]
    public void A_byte_order_mark_in_the_string_path_is_tolerated()
    {
        var text = (char)0xFEFF + TestFixtures.ReadText(TestFixtures.Minimal);

        var file = Mt940Parser.Parse(text);

        Assert.False(file.Report.HasWarnings);
        Assert.Equal("MINIMAL0001", Assert.Single(file.Statements).TransactionReference);
    }

    [Fact]
    public void Empty_text_parses_to_an_empty_file()
    {
        var file = Mt940Parser.Parse(string.Empty);

        Assert.Empty(file.Statements);
    }

    [Fact]
    public void Whitespace_only_text_parses_to_an_empty_file()
    {
        var file = Mt940Parser.Parse("\r\n\r\n  \r\n");

        Assert.Empty(file.Statements);
    }

    [Fact]
    public void Options_default_to_warn_and_raw_information()
    {
        var options = new Mt940Options();

        Assert.Equal(BalanceMismatchBehavior.Warn, options.BalanceMismatchBehavior);
        Assert.Same(RawInformationParser.Instance, options.InformationParser);
    }

    [Fact]
    public void Statement_level_information_after_the_closing_balance_is_kept_separately()
    {
        var text = TestFixtures.ReadText(TestFixtures.BalanceMismatch).TrimEnd('\n')
            + "\n:86:STATEMENT LEVEL NOTE\n";

        var file = Mt940Parser.Parse(text);

        var statement = Assert.Single(file.Statements);
        Assert.Equal("STATEMENT LEVEL NOTE", statement.InformationToAccountOwner);
        Assert.Equal("ONLY DEBIT", Assert.Single(statement.Lines).Information);
    }

    [Fact]
    public void A_940_with_closing_balance_is_not_intraday_and_a_942_is()
    {
        var mt940 = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Minimal));
        var mt942 = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Mt942Intraday));

        Assert.False(Assert.Single(mt940.Statements).IsIntraday);
        Assert.True(Assert.Single(mt942.Statements).IsIntraday);
    }

    [Fact]
    public void Statements_and_collections_are_read_only_views()
    {
        var file = Mt940Parser.Parse(TestFixtures.ReadText(TestFixtures.Minimal));

        Assert.IsAssignableFrom<IReadOnlyList<Mt940Statement>>(file.Statements);
        var statement = Assert.Single(file.Statements);
        Assert.IsAssignableFrom<IReadOnlyList<StatementLine>>(statement.Lines);
        Assert.IsAssignableFrom<IReadOnlyList<Balance>>(statement.ForwardAvailableBalances);
        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(statement.Lines[0].StructuredInformation);
    }

    [Fact]
    public void Balance_signed_amount_negates_debit_balances()
    {
        var balance = new Balance(DebitCreditMark.Debit, new DateOnly(2026, 7, 1), "EUR", 42.50m, false);

        Assert.Equal(-42.50m, balance.SignedAmount);
        Assert.Equal(42.50m, (balance with { Mark = DebitCreditMark.Credit }).SignedAmount);
    }

    [Fact]
    public async Task ParseAsync_and_Parse_agree()
    {
        var text = TestFixtures.ReadText(TestFixtures.Minimal);
        await using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));

        var fromStream = await Mt940Parser.ParseAsync(stream);
        var fromText = Mt940Parser.Parse(text);

        Assert.Equal(fromText.Statements.Count, fromStream.Statements.Count);
        Assert.Equal(
            fromText.Statements[0].ClosingBalance!.Amount,
            fromStream.Statements[0].ClosingBalance!.Amount);
    }
}
