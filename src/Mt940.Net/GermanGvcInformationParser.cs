using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Mt940;

/// <summary>
/// Parses the German structured :86: layout (DK / DFUE-Abkommen Anlage 3, "field 86"), as sent by
/// German and Austrian banks, for example
/// "166?00SEPA-UEBERWEISUNG?20EREF+INV-2024-0042?21SVWZ+Rechnung 42?30GENODEF1S02?31DE02...?32Max Mustermann".
/// The text opens with a 3-digit GVC (Geschaeftsvorfallcode, the booking code) and is then split into
/// sub-fields each introduced by a <c>?NN</c> marker (a question mark plus a two-digit code). The purpose
/// sub-fields (<c>?20</c>-<c>?29</c> and <c>?60</c>-<c>?63</c>) and the counterparty-name sub-fields
/// (<c>?32</c>-<c>?33</c>) are concatenated in the order they appear, with no separator, because the bank
/// has already placed any needed spacing inside each part. Continuation lines are joined before scanning
/// because banks wrap the 65-character :86: lines mid-token. A <c>?NN</c> whose code is not recognized is
/// preserved under the key <c>?NN</c>. This is a sibling of <see cref="SlashDelimitedInformationParser"/>;
/// the raw text always remains on <see cref="StatementLine.Information"/>.
/// </summary>
public sealed class GermanGvcInformationParser : IInformationParser
{
    private const char MarkerChar = '?';
    private const int CodeLength = 2;

    private const int BookingTextCode = 0;
    private const int PrimanotaCode = 10;
    private const int PurposeCodeMin = 20;
    private const int PurposeCodeMax = 29;
    private const int BicCode = 30;
    private const int IbanCode = 31;
    private const int NameCodeMin = 32;
    private const int NameCodeMax = 33;
    private const int TextKeyCode = 34;
    private const int ExtendedPurposeCodeMin = 60;
    private const int ExtendedPurposeCodeMax = 63;

    /// <summary>Key for the leading 3-digit GVC (Geschaeftsvorfallcode, the booking code).</summary>
    public const string GvcKey = "Gvc";

    /// <summary>Key for the booking text (Buchungstext, sub-field <c>?00</c>).</summary>
    public const string BookingTextKey = "BookingText";

    /// <summary>Key for the primanota reference (sub-field <c>?10</c>).</summary>
    public const string PrimanotaKey = "Primanota";

    /// <summary>
    /// Key for the purpose / remittance text (Verwendungszweck): sub-fields <c>?20</c>-<c>?29</c> and
    /// <c>?60</c>-<c>?63</c> concatenated in the order they appear, with no separator.
    /// </summary>
    public const string PurposeKey = "Purpose";

    /// <summary>Key for the counterparty BIC / bank code (sub-field <c>?30</c>).</summary>
    public const string BicKey = "Bic";

    /// <summary>Key for the counterparty IBAN / account (sub-field <c>?31</c>).</summary>
    public const string IbanKey = "Iban";

    /// <summary>
    /// Key for the counterparty name: sub-fields <c>?32</c> and <c>?33</c> concatenated in the order
    /// they appear, with no separator.
    /// </summary>
    public const string NameKey = "Name";

    /// <summary>Key for the text key (Textschluessel, sub-field <c>?34</c>).</summary>
    public const string TextKeyKey = "TextKey";

    /// <summary>Prefix under which an unrecognized <c>?NN</c> sub-field is preserved (for example <c>?40</c>).</summary>
    public const string RawSubFieldKeyPrefix = "?";

    /// <summary>A shared instance.</summary>
    public static GermanGvcInformationParser Default { get; } = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Parse(string information)
    {
        ArgumentNullException.ThrowIfNull(information);
        var content = information.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
        if (content.Length == 0)
        {
            return RawInformationParser.Empty;
        }

        var markers = FindMarkers(content);
        var gvcEnd = markers.Count > 0 ? markers[0].MarkerStart : content.Length;
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (gvcEnd > 0)
        {
            result[GvcKey] = content[..gvcEnd];
        }

        StringBuilder? purpose = null;
        StringBuilder? name = null;
        for (var i = 0; i < markers.Count; i++)
        {
            var (code, valueStart, _) = markers[i];
            var valueEnd = i + 1 < markers.Count ? markers[i + 1].MarkerStart : content.Length;
            var value = content[valueStart..valueEnd];
            var codeValue = int.Parse(code, NumberStyles.None, CultureInfo.InvariantCulture);

            switch (codeValue)
            {
                case BookingTextCode:
                    result[BookingTextKey] = value;
                    break;
                case PrimanotaCode:
                    result[PrimanotaKey] = value;
                    break;
                case BicCode:
                    result[BicKey] = value;
                    break;
                case IbanCode:
                    result[IbanKey] = value;
                    break;
                case TextKeyCode:
                    result[TextKeyKey] = value;
                    break;
                case >= PurposeCodeMin and <= PurposeCodeMax:
                case >= ExtendedPurposeCodeMin and <= ExtendedPurposeCodeMax:
                    (purpose ??= new StringBuilder()).Append(value);
                    break;
                case >= NameCodeMin and <= NameCodeMax:
                    (name ??= new StringBuilder()).Append(value);
                    break;
                default:
                    result[RawSubFieldKeyPrefix + code] = value;
                    break;
            }
        }

        if (purpose is not null)
        {
            result[PurposeKey] = purpose.ToString();
        }

        if (name is not null)
        {
            result[NameKey] = name.ToString();
        }

        return result.Count == 0 ? RawInformationParser.Empty : new ReadOnlyDictionary<string, string>(result);
    }

    private static List<(string Code, int ValueStart, int MarkerStart)> FindMarkers(string content)
    {
        var markers = new List<(string Code, int ValueStart, int MarkerStart)>();
        var i = 0;
        while (i < content.Length)
        {
            if (content[i] != MarkerChar)
            {
                i++;
                continue;
            }

            var codeStart = i + 1;
            var codeEnd = codeStart + CodeLength;
            if (codeEnd <= content.Length && AllAsciiDigits(content, codeStart, codeEnd))
            {
                markers.Add((content[codeStart..codeEnd], codeEnd, i));
                i = codeEnd;
            }
            else
            {
                i++;
            }
        }

        return markers;
    }

    private static bool AllAsciiDigits(string content, int start, int end)
    {
        for (var i = start; i < end; i++)
        {
            if (!char.IsAsciiDigit(content[i]))
            {
                return false;
            }
        }

        return true;
    }
}
