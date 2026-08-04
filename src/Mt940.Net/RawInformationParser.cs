using System.Collections.ObjectModel;

namespace Mt940;

/// <summary>
/// The default <see cref="IInformationParser"/>: performs no structured parsing, so
/// <see cref="StatementLine.StructuredInformation"/> stays empty and only the raw text on
/// <see cref="StatementLine.Information"/> is populated.
/// </summary>
public sealed class RawInformationParser : IInformationParser
{
    internal static readonly IReadOnlyDictionary<string, string> Empty =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

    private RawInformationParser()
    {
    }

    /// <summary>The shared instance.</summary>
    public static RawInformationParser Instance { get; } = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Parse(string information) => Empty;
}
