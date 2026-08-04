namespace Mt940;

/// <summary>
/// Thrown when the input cannot be parsed as MT940/MT942, with the line number and tag where
/// parsing failed. <see cref="Mt940Parser.TryParse(string?, out Mt940File?)"/> never throws.
/// </summary>
public sealed class Mt940ParseException : FormatException
{
    /// <summary>Creates an exception without position context.</summary>
    /// <param name="message">The error description.</param>
    public Mt940ParseException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception pointing at a line and tag in the input.</summary>
    /// <param name="message">The error description.</param>
    /// <param name="lineNumber">The 1-based line number in the input text.</param>
    /// <param name="tag">The tag being parsed, without colons, or null when no tag applies.</param>
    public Mt940ParseException(string message, int lineNumber, string? tag)
        : base(FormatMessage(message, lineNumber, tag))
    {
        LineNumber = lineNumber;
        Tag = tag;
    }

    /// <summary>The 1-based line number in the input text, or 0 when unknown.</summary>
    public int LineNumber { get; }

    /// <summary>The tag being parsed when the error occurred, without colons, or null.</summary>
    public string? Tag { get; }

    private static string FormatMessage(string message, int lineNumber, string? tag) =>
        tag is null
            ? $"{message} (line {lineNumber})"
            : $"{message} (line {lineNumber}, tag :{tag}:)";
}
