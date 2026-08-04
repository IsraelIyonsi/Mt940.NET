namespace Mt940;

/// <summary>Everything the parser wants you to know about a parse that still succeeded.</summary>
public sealed class ParseReport
{
    internal ParseReport(IReadOnlyList<ParseWarning> warnings)
    {
        Warnings = warnings;
    }

    /// <summary>All warnings, in the order they were raised.</summary>
    public IReadOnlyList<ParseWarning> Warnings { get; }

    /// <summary>True when at least one warning was raised.</summary>
    public bool HasWarnings => Warnings.Count > 0;
}
