namespace Mt940;

/// <summary>A tag the parser did not recognize, preserved verbatim rather than dropped.</summary>
/// <param name="Tag">The tag name without colons, for example "NS" or "23E".</param>
/// <param name="Value">The raw field value, with continuation lines joined by '\n'.</param>
/// <param name="LineNumber">The 1-based line number of the tag in the input text.</param>
public sealed record RawTag(string Tag, string Value, int LineNumber);
