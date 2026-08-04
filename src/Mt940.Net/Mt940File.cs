namespace Mt940;

/// <summary>The result of parsing one MT940/MT942 text: the statements and the parse report.</summary>
public sealed class Mt940File
{
    internal Mt940File(IReadOnlyList<Mt940Statement> statements, ParseReport report)
    {
        Statements = statements;
        Report = report;
    }

    /// <summary>All statements in the file, in file order. A file may contain many.</summary>
    public IReadOnlyList<Mt940Statement> Statements { get; }

    /// <summary>Warnings raised during parsing: unknown tags, unreconciled balances, stray lines.</summary>
    public ParseReport Report { get; }
}
