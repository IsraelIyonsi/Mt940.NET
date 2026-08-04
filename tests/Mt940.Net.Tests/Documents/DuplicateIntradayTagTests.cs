namespace Mt940.Tests.Documents;

/// <summary>
/// Duplicate MT942 tags must follow the same keep-first-and-warn discipline as the MT940 tags.
/// </summary>
public sealed class DuplicateIntradayTagTests
{
    private static Mt940File ParseIntraday(string extraFields)
    {
        var text = $"""
            :20:DUP942
            :25:1234567890
            :28C:1
            :13D:2607211330+0200
            :34F:EURD5,
            :34F:EURC7,
            :90D:1EUR200,50
            :90C:2EUR1500,00
            {extraFields}
            """;
        return Mt940Parser.Parse(text);
    }

    [Fact]
    public void Duplicate_13D_keeps_the_first_and_warns()
    {
        var file = ParseIntraday(":13D:2607211500+0200");

        var statement = Assert.Single(file.Statements);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 21, 13, 30, 0, TimeSpan.FromHours(2)),
            statement.ReportDateTime);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("13D", warning.Tag);
        Assert.Contains("Duplicate", warning.Message);
    }

    [Fact]
    public void Duplicate_90D_keeps_the_first_and_warns()
    {
        var file = ParseIntraday(":90D:9EUR999,99");

        var statement = Assert.Single(file.Statements);
        Assert.Equal(new EntrySummary(1, "EUR", 200.50m), statement.DebitEntrySummary);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("90D", warning.Tag);
        Assert.Contains("Duplicate", warning.Message);
    }

    [Fact]
    public void Duplicate_90C_keeps_the_first_and_warns()
    {
        var file = ParseIntraday(":90C:9EUR999,99");

        var statement = Assert.Single(file.Statements);
        Assert.Equal(new EntrySummary(2, "EUR", 1500.00m), statement.CreditEntrySummary);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("90C", warning.Tag);
        Assert.Contains("Duplicate", warning.Message);
    }

    [Fact]
    public void Duplicate_debit_floor_limit_keeps_the_first_and_warns()
    {
        var file = ParseIntraday(":34F:EURD9,");

        var statement = Assert.Single(file.Statements);
        Assert.Equal(5m, statement.DebitFloorLimit!.Amount);
        Assert.Equal(7m, statement.CreditFloorLimit!.Amount);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("34F", warning.Tag);
        Assert.Contains("Duplicate", warning.Message);
    }

    [Fact]
    public void Duplicate_credit_floor_limit_keeps_the_first_and_warns()
    {
        var file = ParseIntraday(":34F:EURC9,");

        var statement = Assert.Single(file.Statements);
        Assert.Equal(7m, statement.CreditFloorLimit!.Amount);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("34F", warning.Tag);
    }

    [Fact]
    public void Third_unmarked_floor_limit_keeps_the_first_two_and_warns()
    {
        var file = ParseIntraday(":34F:EUR9,");

        var statement = Assert.Single(file.Statements);
        Assert.Equal(5m, statement.DebitFloorLimit!.Amount);
        Assert.Equal(7m, statement.CreditFloorLimit!.Amount);
        var warning = Assert.Single(file.Report.Warnings);
        Assert.Equal("34F", warning.Tag);
    }

    [Fact]
    public void Non_duplicated_intraday_tags_do_not_warn()
    {
        var file = ParseIntraday(":61:2607210721C1500,00NTRFE2E-42");

        Assert.False(file.Report.HasWarnings);
    }
}
