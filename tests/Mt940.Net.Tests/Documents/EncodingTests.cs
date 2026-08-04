using System.Text;
using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class EncodingTests
{
    private const string ExpectedDescription = "MIETE BÜRO MÜLLER STRAßE 12 CAFÉ LØN";

    [Fact]
    public async Task Latin1_encoded_stream_falls_back_from_utf8_and_decodes_correctly()
    {
        await using var stream = TestFixtures.OpenStream(TestFixtures.Latin1Characters);

        var file = await Mt940Parser.ParseAsync(stream);

        Assert.False(file.Report.HasWarnings);
        var statement = Assert.Single(file.Statements);
        Assert.Equal(ExpectedDescription, Assert.Single(statement.Lines).Information);
        Assert.Equal(200.00m, statement.ClosingBalance!.Amount);
    }

    [Fact]
    public async Task Utf8_encoded_stream_decodes_as_utf8()
    {
        var latin1Bytes = await File.ReadAllBytesAsync(TestFixtures.PathOf(TestFixtures.Latin1Characters));
        var text = Encoding.Latin1.GetString(latin1Bytes);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var file = await Mt940Parser.ParseAsync(stream);

        var statement = Assert.Single(file.Statements);
        Assert.Equal(ExpectedDescription, Assert.Single(statement.Lines).Information);
    }

    [Fact]
    public async Task Utf8_byte_order_mark_is_tolerated()
    {
        var body = TestFixtures.ReadText(TestFixtures.Minimal);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(body)).ToArray();
        await using var stream = new MemoryStream(bytes);

        var file = await Mt940Parser.ParseAsync(stream);

        Assert.Equal("MINIMAL0001", Assert.Single(file.Statements).TransactionReference);
    }

    [Fact]
    public async Task ParseAsync_honors_cancellation()
    {
        await using var stream = TestFixtures.OpenStream(TestFixtures.Minimal);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Mt940Parser.ParseAsync(stream, cancellationToken: cancelled.Token));
    }
}
