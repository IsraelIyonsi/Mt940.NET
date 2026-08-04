using System.Globalization;

namespace Mt940.Tests.Support;

internal static class TestFixtures
{
    public const string Minimal = "minimal.sta";
    public const string MultiStatement = "multi-statement.sta";
    public const string BlockWrapped = "block-wrapped.sta";
    public const string Mt942Intraday = "mt942-intraday.sta";
    public const string SepaStructured = "sepa-structured.sta";
    public const string NoDecimalAmounts = "no-decimal-amounts.sta";
    public const string ReversalMarks = "reversal-marks.sta";
    public const string YearRollover = "year-rollover.sta";
    public const string Multiline86 = "multiline-86.sta";
    public const string UnknownTags = "unknown-tags.sta";
    public const string Latin1Characters = "latin1-characters.sta";
    public const string BalanceMismatch = "balance-mismatch.sta";

    public static readonly string[] All =
    [
        Minimal, MultiStatement, BlockWrapped, Mt942Intraday, SepaStructured, NoDecimalAmounts,
        ReversalMarks, YearRollover, Multiline86, UnknownTags, BalanceMismatch,
    ];

    public static readonly string[] Reconcilable =
    [
        Minimal, MultiStatement, BlockWrapped, SepaStructured, NoDecimalAmounts,
        ReversalMarks, YearRollover, Multiline86, UnknownTags,
    ];

    public static string PathOf(string name) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", name);

    public static string ReadText(string name) =>
        File.ReadAllText(PathOf(name));

    public static FileStream OpenStream(string name) =>
        File.OpenRead(PathOf(name));

    public static void RunWithCulture(string cultureName, Action action)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
