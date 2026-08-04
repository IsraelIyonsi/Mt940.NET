using System.Diagnostics;
using System.Text;

namespace Mt940.Tests.Performance;

/// <summary>
/// Block-wrapped multi-message files are what banks actually deliver, so parsing them must scale
/// linearly. A quadratic parse doubles to roughly 4x per size doubling; this asserts well under that.
/// </summary>
public sealed class LinearityTests
{
    private const int BaseMessageCount = 3000;
    private const int DoubledMessageCount = BaseMessageCount * 2;
    private const double MaxElapsedRatioForDoubling = 3.0;
    private const int MaxDoubledElapsedMilliseconds = 5000;
    private const int TimedRuns = 3;

    [Fact]
    public void Block_wrapped_parsing_scales_linearly_with_message_count()
    {
        var baseText = BuildBlockWrappedFile(BaseMessageCount);
        var doubledText = BuildBlockWrappedFile(DoubledMessageCount);

        Mt940Parser.Parse(baseText); // warmup: JIT + regex compilation

        var baseElapsed = BestOf(TimedRuns, baseText, BaseMessageCount);
        var doubledElapsed = BestOf(TimedRuns, doubledText, DoubledMessageCount);

        Assert.True(
            doubledElapsed < MaxDoubledElapsedMilliseconds,
            $"Parsing {DoubledMessageCount} block-wrapped messages took {doubledElapsed} ms.");

        var ratio = doubledElapsed / Math.Max(1.0, baseElapsed);
        Assert.True(
            ratio < MaxElapsedRatioForDoubling,
            $"Doubling the message count scaled elapsed time by {ratio:F2}x " +
            $"({baseElapsed} ms -> {doubledElapsed} ms); expected under {MaxElapsedRatioForDoubling}x.");
    }

    private static double BestOf(int runs, string text, int expectedStatements)
    {
        var best = double.MaxValue;
        for (var i = 0; i < runs; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var file = Mt940Parser.Parse(text);
            stopwatch.Stop();
            Assert.Equal(expectedStatements, file.Statements.Count);
            best = Math.Min(best, stopwatch.Elapsed.TotalMilliseconds);
        }

        return best;
    }

    private static string BuildBlockWrappedFile(int messages)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < messages; i++)
        {
            builder
                .Append("{1:F01BANKBEBBAXXX0000000000}{2:I940BANKDEFFXXXXN}{4:\r\n")
                .Append(":20:REF").Append(i).Append("\r\n")
                .Append(":25:1234567890\r\n")
                .Append(":28C:1\r\n")
                .Append(":60F:C260101EUR0,\r\n")
                .Append(":61:2601020102C1,00NTRFNONREF\r\n")
                .Append(":86:GENERATED LINE FOR LINEARITY MEASUREMENT\r\n")
                .Append(":62F:C260102EUR1,00\r\n")
                .Append("-}");
        }

        return builder.ToString();
    }
}
