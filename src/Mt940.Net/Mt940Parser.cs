using System.Text;
using Mt940.Internal;

namespace Mt940;

/// <summary>Parses SWIFT MT940 customer statements and MT942 interim transaction reports.</summary>
public static class Mt940Parser
{
    /// <summary>
    /// Parses MT940/MT942 text into statements. Accepts bare tag streams as well as messages
    /// wrapped in {1:}{2:}{3:}{4:...-} blocks, CRLF or LF line endings, and multiple statements
    /// per input. Recoverable problems (unknown tags, unreconciled balances, stray lines) become
    /// warnings on <see cref="Mt940File.Report"/>; structurally invalid fields throw.
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

    /// <summary>Attempts to parse MT940/MT942 text with options. Never throws, whatever the input.</summary>
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
        catch (Mt940ParseException)
        {
            return false;
        }
    }

    /// <summary>
    /// Reads and parses MT940/MT942 text from a stream. The bytes are decoded as UTF-8 when they
    /// are valid UTF-8 and as ISO-8859-1 (Latin-1) otherwise, which covers the encodings banks
    /// actually emit; a UTF-8 byte order mark is tolerated.
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
        return Parse(DecodeText(buffer.ToArray()), options);
    }

    private static string DecodeText(byte[] bytes)
    {
        var strictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        string text;
        try
        {
            text = strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            text = Encoding.Latin1.GetString(bytes);
        }

        const char byteOrderMark = (char)0xFEFF;
        return text.TrimStart(byteOrderMark);
    }
}
