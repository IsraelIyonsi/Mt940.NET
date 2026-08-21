namespace Mt940;

/// <summary>Options controlling how <see cref="Mt940Parser"/> parses.</summary>
public sealed class Mt940Options
{
    internal static readonly Mt940Options Default = new();

    /// <summary>
    /// What to do when opening balance plus the sum of signed line amounts differs from the
    /// closing balance. Defaults to <see cref="BalanceMismatchBehavior.Warn"/>.
    /// </summary>
    public BalanceMismatchBehavior BalanceMismatchBehavior { get; init; } = BalanceMismatchBehavior.Warn;

    /// <summary>
    /// The strategy used to parse each statement line's :86: information into structured sub-fields.
    /// Defaults to <see cref="RawInformationParser.Instance"/>, which leaves the text raw.
    /// Use <see cref="SlashDelimitedInformationParser"/> for the SEPA-style /TAG/value convention,
    /// <see cref="GermanGvcInformationParser"/> for the German GVC / ?NN layout, or implement
    /// <see cref="IInformationParser"/> for another bank-specific dialect.
    /// </summary>
    public IInformationParser InformationParser { get; init; } = RawInformationParser.Instance;
}
