using System.Text;
using Mt940.Tests.Support;

namespace Mt940.Tests.Documents;

public sealed class EncodingTests
{
    private const string ExpectedDescription = "MIETE BÜRO MÜLLER STRAßE 12 CAFÉ LØN";

    [Fact]
    public async Task Non_utf8_stream_falls_back_to_windows_1252_and_decodes_correctly()
    {
        await using var stream = TestFixtures.OpenStream(TestFixtures.Latin1Characters);

        var file = await Mt940Parser.ParseAsync(stream);

        Assert.False(file.Report.HasWarnings);
        var statement = Assert.Single(file.Statements);
        Assert.Equal(ExpectedDescription, Assert.Single(statement.Lines).Information);
        Assert.Equal(200.00m, statement.ClosingBalance!.Amount);
    }

    [Fact]
    public async Task Windows_1252_control_range_bytes_decode_to_euro_and_curly_quotes()
    {
        const byte euroSignByte = 0x80;
        const byte rightSingleQuoteByte = 0x92;
        var head = Encoding.ASCII.GetBytes(
            ":20:WIN1252\n:25:1\n:28C:1\n:60F:C260101EUR9,00\n:61:2601020102D9,00NTRFNONREF\n:86:PRICE ");
        var tail = Encoding.ASCII.GetBytes(" JAN\n:62F:C260101EUR0,\n");
        var bytes = head
            .Concat(new[] { euroSignByte, (byte)' ', rightSingleQuoteByte })
            .Concat(tail)
            .ToArray();
        await using var stream = new MemoryStream(bytes);

        var file = await Mt940Parser.ParseAsync(stream);

        var line = Assert.Single(Assert.Single(file.Statements).Lines);
        Assert.Equal("PRICE € ’ JAN", line.Information);
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
