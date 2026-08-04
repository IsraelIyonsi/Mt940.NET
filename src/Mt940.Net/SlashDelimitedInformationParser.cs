using System.Collections.ObjectModel;

namespace Mt940;

/// <summary>
/// Parses the widely used /TAG/value convention in :86: fields (SEPA-style structured information
/// as sent by ABN AMRO, ING, Rabobank, and many other European banks), for example
/// "/EREF/E2E-42/MARF/MND-001/BENM//NAME/ACME BV/REMI/INVOICE 42".
/// A sub-field starts at /TAG/ where TAG is one of the recognized sub-tags and runs until the next
/// recognized sub-tag or the end of the text. Continuation lines are joined before scanning because
/// banks wrap the 65-character :86: lines mid-token. Bank dialects vary: pass your own sub-tag set
/// to the constructor, or implement <see cref="IInformationParser"/> for anything more exotic.
/// </summary>
public sealed class SlashDelimitedInformationParser : IInformationParser
{
    private const char Delimiter = '/';

    private static readonly string[] DefaultSubTagList =
    [
        "ADDR", "BENM", "BIC", "CHGS", "CNTP", "CREF", "CSID", "EREF", "EXCH", "IBAN", "ID",
        "ISDT", "KREF", "MARF", "NAME", "NRTX", "ORDP", "PREF", "PURP", "REMI", "RTRN",
        "SVCL", "TRTP", "ULTB", "ULTD",
    ];

    private readonly HashSet<string> _subTags;

    /// <summary>Creates a parser recognizing <see cref="DefaultSubTags"/>.</summary>
    public SlashDelimitedInformationParser()
        : this(DefaultSubTagList)
    {
    }

    /// <summary>Creates a parser recognizing only the given sub-tags.</summary>
    /// <param name="subTags">The sub-tags to recognize between slashes, for example "EREF".</param>
    /// <exception cref="ArgumentNullException">When <paramref name="subTags"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="subTags"/> is empty or contains
    /// a null, empty, or slash-containing entry.</exception>
    public SlashDelimitedInformationParser(IEnumerable<string> subTags)
    {
        ArgumentNullException.ThrowIfNull(subTags);
        _subTags = new HashSet<string>(subTags, StringComparer.Ordinal);
        if (_subTags.Count == 0)
        {
            throw new ArgumentException("At least one sub-tag is required.", nameof(subTags));
        }

        foreach (var subTag in _subTags)
        {
            if (string.IsNullOrEmpty(subTag) || subTag.Contains(Delimiter))
            {
                throw new ArgumentException(
                    $"Sub-tags must be non-empty and must not contain '{Delimiter}'.", nameof(subTags));
            }
        }
    }

    /// <summary>A shared instance recognizing <see cref="DefaultSubTags"/>.</summary>
    public static SlashDelimitedInformationParser Default { get; } = new();

    /// <summary>
    /// The sub-tags recognized by the parameterless constructor: the common SEPA code words
    /// (EREF, KREF, MARF, PREF, CREF, TRTP, REMI, PURP, RTRN, and the counterparty group
    /// BENM/ORDP/CNTP with NAME, IBAN, BIC, ADDR, ID, plus CSID, ISDT, EXCH, CHGS, SVCL,
    /// NRTX, ULTB, ULTD).
    /// </summary>
    public static IReadOnlyList<string> DefaultSubTags { get; } = Array.AsReadOnly(DefaultSubTagList);

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Parse(string information)
    {
        ArgumentNullException.ThrowIfNull(information);
        var content = information.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var markers = FindMarkers(content);
        if (markers.Count == 0)
        {
            return RawInformationParser.Empty;
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < markers.Count; i++)
        {
            var (subTag, valueStart, _) = markers[i];
            var valueEnd = i + 1 < markers.Count ? markers[i + 1].TagStart : content.Length;
            result[subTag] = content[valueStart..valueEnd];
        }

        return new ReadOnlyDictionary<string, string>(result);
    }

    private List<(string SubTag, int ValueStart, int TagStart)> FindMarkers(string content)
    {
        var markers = new List<(string SubTag, int ValueStart, int TagStart)>();
        var i = 0;
        while (i < content.Length)
        {
            if (content[i] != Delimiter)
            {
                i++;
                continue;
            }

            var close = content.IndexOf(Delimiter, i + 1);
            if (close < 0)
            {
                break;
            }

            var candidate = content[(i + 1)..close];
            if (_subTags.Contains(candidate))
            {
                markers.Add((candidate, close + 1, i));
                i = close;
            }
            else
            {
                i++;
            }
        }

        return markers;
    }
}
