using System.Globalization;
using System.Text.RegularExpressions;

namespace Mt940.Internal;

internal static partial class FieldParsers
{
    private const int PivotTwoDigitYear = 80;
    private const int CenturyBeforePivot = 2000;
    private const int CenturyFromPivot = 1900;
    private const char SwiftDecimalSeparator = ',';
    private const char InvariantDecimalSeparator = '.';
    private const string ReferenceSeparator = "//";
    private const int MaxCustomerReferenceLength = 16;
    private const int MaxUtcOffsetHours = 14;

    [GeneratedRegex(@"^(?:\d+,?\d*|,\d+)$")]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"^(?<mark>C|D)(?<date>\d{6})(?<currency>[A-Z]{3})(?<amount>.+)$")]
    private static partial Regex BalancePattern();

    [GeneratedRegex(@"^(?<number>\d{1,5})(?:/(?<sequence>\d{1,5}))?$")]
    private static partial Regex StatementNumberPattern();

    [GeneratedRegex(
        @"^(?<valueDate>\d{6})(?<entryDate>\d{4})?(?<mark>RC|RD|C|D)(?<funds>[A-Z])?(?<amount>\d+,?\d*|,\d+)(?<type>[NSF][A-Z0-9]{3})(?<rest>.*)$")]
    private static partial Regex StatementLinePattern();

    [GeneratedRegex(@"^(?<date>\d{6})(?<time>\d{4})(?<sign>[+-])(?<offset>\d{4})$")]
    private static partial Regex DateTimeIndicationPattern();

    [GeneratedRegex(@"^(?<currency>[A-Z]{3})(?<mark>C|D)?(?<amount>.+)$")]
    private static partial Regex FloorLimitPattern();

    [GeneratedRegex(@"^(?<count>\d{1,5})(?<currency>[A-Z]{3})(?<amount>.+)$")]
    private static partial Regex EntrySummaryPattern();

    internal static decimal ParseAmount(string raw, int lineNumber, string tag)
    {
        if (!AmountPattern().IsMatch(raw))
        {
            throw new Mt940ParseException(
                $"Invalid SWIFT amount \"{raw}\": expected digits with an optional comma decimal separator",
                lineNumber, tag);
        }

        var invariant = raw.Replace(SwiftDecimalSeparator, InvariantDecimalSeparator);
        if (!decimal.TryParse(invariant, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
        {
            throw new Mt940ParseException($"Amount \"{raw}\" is out of range", lineNumber, tag);
        }

        return amount;
    }

    internal static DateOnly ParseDate(string yymmdd, int lineNumber, string tag)
    {
        if (yymmdd.Length != 6 || !int.TryParse(yymmdd, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            throw new Mt940ParseException($"Invalid date \"{yymmdd}\": expected YYMMDD", lineNumber, tag);
        }

        var twoDigitYear = ToInt(yymmdd[..2]);
        var year = twoDigitYear >= PivotTwoDigitYear
            ? CenturyFromPivot + twoDigitYear
            : CenturyBeforePivot + twoDigitYear;
        var month = ToInt(yymmdd[2..4]);
        var day = ToInt(yymmdd[4..6]);
        try
        {
            return new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new Mt940ParseException($"Date \"{yymmdd}\" is not a calendar date", lineNumber, tag);
        }
    }

    internal static DateOnly ResolveEntryDate(DateOnly valueDate, string monthDay, int lineNumber)
    {
        if (monthDay.Length != 4 || !int.TryParse(monthDay, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            throw new Mt940ParseException(
                $"Invalid entry date \"{monthDay}\": expected MMDD", lineNumber, TagNames.StatementLine);
        }

        var month = ToInt(monthDay[..2]);
        var day = ToInt(monthDay[2..4]);
        DateOnly? best = null;
        var bestDistance = int.MaxValue;
        for (var year = valueDate.Year - 1; year <= valueDate.Year + 1; year++)
        {
            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            {
                continue;
            }

            var candidate = new DateOnly(year, month, day);
            var distance = Math.Abs(candidate.DayNumber - valueDate.DayNumber);
            var winsTie = distance == bestDistance && year == valueDate.Year;
            if (distance < bestDistance || winsTie)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best ?? throw new Mt940ParseException(
            $"Entry date \"{monthDay}\" is not a calendar date near {valueDate:yyyy-MM-dd}",
            lineNumber, TagNames.StatementLine);
    }

    internal static Balance ParseBalance(string value, int lineNumber, string tag, bool isIntermediate)
    {
        var match = BalancePattern().Match(FirstLine(value));
        if (!match.Success)
        {
            throw new Mt940ParseException(
                $"Invalid balance \"{value}\": expected C/D mark, YYMMDD date, currency, and amount",
                lineNumber, tag);
        }

        var mark = match.Groups["mark"].Value == "D" ? DebitCreditMark.Debit : DebitCreditMark.Credit;
        return new Balance(
            mark,
            ParseDate(match.Groups["date"].Value, lineNumber, tag),
            match.Groups["currency"].Value,
            ParseAmount(match.Groups["amount"].Value, lineNumber, tag),
            isIntermediate);
    }

    internal static (string Number, string? Sequence) ParseStatementNumber(string value, int lineNumber, string tag)
    {
        var match = StatementNumberPattern().Match(FirstLine(value));
        if (!match.Success)
        {
            throw new Mt940ParseException(
                $"Invalid statement number \"{value}\": expected digits with an optional /sequence",
                lineNumber, tag);
        }

        var sequence = match.Groups["sequence"];
        return (match.Groups["number"].Value, sequence.Success ? sequence.Value : null);
    }

    internal static StatementLineDraft ParseStatementLine(string value, int lineNumber)
    {
        var newlineIndex = value.IndexOf('\n');
        var head = newlineIndex < 0 ? value : value[..newlineIndex];
        var supplementary = newlineIndex < 0 ? null : value[(newlineIndex + 1)..];

        var match = StatementLinePattern().Match(head);
        if (!match.Success)
        {
            throw new Mt940ParseException(
                $"Invalid statement line \"{head}\": expected value date, optional entry date, " +
                "C/D/RC/RD mark, optional funds code, amount, and transaction type",
                lineNumber, TagNames.StatementLine);
        }

        var valueDate = ParseDate(match.Groups["valueDate"].Value, lineNumber, TagNames.StatementLine);
        var entryDateGroup = match.Groups["entryDate"];
        var entryDate = entryDateGroup.Success
            ? ResolveEntryDate(valueDate, entryDateGroup.Value, lineNumber)
            : (DateOnly?)null;
        var mark = match.Groups["mark"].Value switch
        {
            "C" => DebitCreditMark.Credit,
            "D" => DebitCreditMark.Debit,
            "RC" => DebitCreditMark.ReversalCredit,
            _ => DebitCreditMark.ReversalDebit,
        };
        var fundsGroup = match.Groups["funds"];
        var (customerReference, bankReference) = SplitReferences(match.Groups["rest"].Value);

        return new StatementLineDraft
        {
            LineNumber = lineNumber,
            ValueDate = valueDate,
            EntryDate = entryDate,
            Mark = mark,
            FundsCode = fundsGroup.Success ? fundsGroup.Value[0] : null,
            Amount = ParseAmount(match.Groups["amount"].Value, lineNumber, TagNames.StatementLine),
            TransactionType = match.Groups["type"].Value,
            CustomerReference = customerReference,
            BankReference = bankReference,
            SupplementaryDetails = supplementary,
        };
    }

    internal static DateTimeOffset ParseDateTimeIndication(string value, int lineNumber, string tag)
    {
        var match = DateTimeIndicationPattern().Match(FirstLine(value));
        if (!match.Success)
        {
            throw new Mt940ParseException(
                $"Invalid date/time indication \"{value}\": expected YYMMDDHHMM+HHMM or YYMMDDHHMM-HHMM",
                lineNumber, tag);
        }

        var date = ParseDate(match.Groups["date"].Value, lineNumber, tag);
        var time = match.Groups["time"].Value;
        var offsetText = match.Groups["offset"].Value;
        var hour = ToInt(time[..2]);
        var minute = ToInt(time[2..4]);
        var offsetHours = ToInt(offsetText[..2]);
        var offsetMinutes = ToInt(offsetText[2..4]);
        if (hour > 23 || minute > 59 || offsetHours > MaxUtcOffsetHours || offsetMinutes > 59)
        {
            throw new Mt940ParseException(
                $"Date/time indication \"{value}\" has an impossible time or offset", lineNumber, tag);
        }

        var offset = new TimeSpan(offsetHours, offsetMinutes, 0);
        if (match.Groups["sign"].Value == "-")
        {
            offset = offset.Negate();
        }

        try
        {
            return new DateTimeOffset(date.Year, date.Month, date.Day, hour, minute, 0, offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new Mt940ParseException(
                $"Date/time indication \"{value}\" is out of range", lineNumber, tag);
        }
    }

    internal static (FloorLimit Limit, DebitCreditMark? Mark) ParseFloorLimit(string value, int lineNumber, string tag)
    {
        var match = FloorLimitPattern().Match(FirstLine(value));
        if (!match.Success)
        {
            throw new Mt940ParseException(
                $"Invalid floor limit \"{value}\": expected currency, optional C/D mark, and amount",
                lineNumber, tag);
        }

        var markGroup = match.Groups["mark"];
        var mark = markGroup.Success
            ? markGroup.Value == "D" ? DebitCreditMark.Debit : DebitCreditMark.Credit
            : (DebitCreditMark?)null;
        return (
            new FloorLimit(
                match.Groups["currency"].Value,
                ParseAmount(match.Groups["amount"].Value, lineNumber, tag)),
            mark);
    }

    internal static EntrySummary ParseEntrySummary(string value, int lineNumber, string tag)
    {
        var match = EntrySummaryPattern().Match(FirstLine(value));
        if (!match.Success)
        {
            throw new Mt940ParseException(
                $"Invalid number and sum of entries \"{value}\": expected count, currency, and amount",
                lineNumber, tag);
        }

        return new EntrySummary(
            ToInt(match.Groups["count"].Value),
            match.Groups["currency"].Value,
            ParseAmount(match.Groups["amount"].Value, lineNumber, tag));
    }

    internal static (string Customer, string? Bank) SplitReferences(string rest)
    {
        var separatorIndex = rest.IndexOf(ReferenceSeparator, StringComparison.Ordinal);
        return separatorIndex < 0
            ? (rest, null)
            : (rest[..separatorIndex], rest[(separatorIndex + ReferenceSeparator.Length)..]);
    }

    internal static bool ExceedsCustomerReferenceLength(string customerReference) =>
        customerReference.Length > MaxCustomerReferenceLength;

    private static string FirstLine(string value)
    {
        var newlineIndex = value.IndexOf('\n');
        return newlineIndex < 0 ? value : value[..newlineIndex];
    }

    private static int ToInt(string digits) =>
        int.Parse(digits, NumberStyles.None, CultureInfo.InvariantCulture);
}
