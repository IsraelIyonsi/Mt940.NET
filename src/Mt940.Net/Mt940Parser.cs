using System.Text;
using Mt940.Internal;

namespace Mt940;

/// <summary>Parses SWIFT MT940 customer statements and MT942 interim transaction reports.</summary>
public static class Mt940Parser
{
    /// <remarks>
    /// Windows-1252 code points for bytes 0x80-0x9F, where it differs from ISO-8859-1: the euro
    /// sign, typographic quotes and dashes, and friends. Bytes Windows-1252 leaves undefined
    /// (0x81, 0x8D, 0x8F, 0x90, 0x9D) pass through as their Latin-1 control characters.
    /// </remarks>
    private static readonly char[] Windows1252ControlRangeMap =
    [
        (char)0x20AC, (char)0x0081, (char)0x201A, (char)0x0192,
        (char)0x201E, (char)0x2026, (char)0x2020, (char)0x2021,
        (char)0x02C6, (char)0x2030, (char)0x0160, (char)0x2039,
        (char)0x0152, (char)0x008D, (char)0x017D, (char)0x008F,
        (char)0x0090, (char)0x2018, (char)0x2019, (char)0x201C,
        (char)0x201D, (char)0x2022, (char)0x2013, (char)0x2014,
        (char)0x02DC, (char)0x2122, (char)0x0161, (char)0x203A,
        (char)0x0153, (char)0x009D, (char)0x017E, (char)0x0178,
    ];

    /// <summary>
    /// Parses MT940/MT942 text into statements. Accepts bare tag streams as well as messages
    /// wrapped in {1:}{2:}{3:}{4:...-} blocks, CRLF, LF, or lone-CR line endings, a leading
    /// byte order mark, and multiple statements per input. Recoverable problems (unknown tags,
    /// unreconciled balances, stray lines) become warnings on <see cref="Mt940File.Report"/>;
    /// structurally invalid fields throw.
    /// </summary>
    /// <param name="text">The MT940/MT942 text.</param>
    /// <param name="options">Parsing options, or null for defaults.</param>
    /// <returns>The parsed file. Never null.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="text"/> is null.</exception>
    /// <exception cref="Mt940ParseException">When a field is structurally invalid, or when balances
    /// do not reconcile and <see cref="Mt940Options.BalanceMismatchBehavior"/> is
    /// <see cref="BalanceMismatchBehavior.Throw"/>.</exception>
    public static Mt940File Parse(string text, Mt940Options? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Mt940Engine.Parse(text, options ?? Mt940Options.Default);
    }

    /// <summary>Attempts to parse MT940/MT942 text. Never throws, whatever the input.</summary>
    /// <param name="text">The MT940/MT942 text, or null.</param>
    /// <param name="file">The parsed file when parsing succeeded, otherwise null.</param>
    /// <returns>True when parsing succeeded.</returns>
    public static bool TryParse(string? text, out Mt940File? file) =>
        TryParse(text, null, out file);

    /// <summary>
    /// Attempts to parse MT940/MT942 text with options. Never throws, whatever the input:
    /// any failure, including one thrown by a custom <see cref="Mt940Options.InformationParser"/>,
    /// yields false rather than an exception.
    /// </summary>
    /// <param name="text">The MT940/MT942 text, or null.</param>
    /// <param name="options">Parsing options, or null for defaults.</param>
    /// <param name="file">The parsed file when parsing succeeded, otherwise null.</param>
    /// <returns>True when parsing succeeded.</returns>
    public static bool TryParse(string? text, Mt940Options? options, out Mt940File? file)
    {
        file = null;
        if (text is null)
        {
            return false;
        }

        try
        {
            file = Mt940Engine.Parse(text, options ?? Mt940Options.Default);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads and parses MT940/MT942 text from a stream. The bytes are decoded as UTF-8 when they
    /// are valid UTF-8 and as Windows-1252 otherwise (a superset of printable ISO-8859-1 that adds
    /// the euro sign and typographic quotes banks actually emit); a UTF-8 byte order mark is
    /// tolerated.
    /// </summary>
    /// <param name="stream">The stream to read to its end.</param>
    /// <param name="options">Parsing options, or null for defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The parsed file. Never null.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="stream"/> is null.</exception>
    /// <exception cref="Mt940ParseException">When a field is structurally invalid, or when balances
    /// do not reconcile and <see cref="Mt940Options.BalanceMismatchBehavior"/> is
    /// <see cref="BalanceMismatchBehavior.Throw"/>.</exception>
    public static async Task<Mt940File> ParseAsync(
        Stream stream,
        Mt940Options? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Parse(DecodeText(buffer.GetBuffer().AsSpan(0, (int)buffer.Length)), options);
    }

    private static string DecodeText(ReadOnlySpan<byte> bytes)
    {
        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            return strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return DecodeWindows1252(bytes);
        }
    }

    private static string DecodeWindows1252(ReadOnlySpan<byte> bytes)
    {
        const byte controlRangeStart = 0x80;
        const byte controlRangeEnd = 0x9F;

        var characters = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
        {
            var value = bytes[i];
            characters[i] = value is >= controlRangeStart and <= controlRangeEnd
                ? Windows1252ControlRangeMap[value - controlRangeStart]
                : (char)value;
        }

        return new string(characters);
    }
}
