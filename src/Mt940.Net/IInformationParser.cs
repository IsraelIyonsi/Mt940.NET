namespace Mt940;

/// <summary>
/// Strategy for parsing the :86: information-to-account-owner text into structured sub-fields.
/// Bank dialects vary widely; this interface is the extension point for supporting yours.
/// The raw text always remains available on <see cref="StatementLine.Information"/>.
/// </summary>
public interface IInformationParser
{
    /// <summary>Parses the raw :86: text (continuation lines joined by '\n') into sub-fields.</summary>
    /// <param name="information">The raw :86: text of one statement line.</param>
    /// <returns>The parsed sub-fields, empty when none were recognized. Never null.</returns>
    IReadOnlyDictionary<string, string> Parse(string information);
}
