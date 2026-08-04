namespace Mt940;

/// <summary>One statement (MT940) or interim transaction report (MT942) from a parsed file.</summary>
public sealed class Mt940Statement
{
    internal Mt940Statement(
        string transactionReference,
        string? relatedReference,
        string account,
        string statementNumber,
        string? sequenceNumber,
        Balance? openingBalance,
        Balance? closingBalance,
        Balance? closingAvailableBalance,
        IReadOnlyList<Balance> forwardAvailableBalances,
        IReadOnlyList<StatementLine> lines,
        bool isIntraday,
        DateTimeOffset? reportDateTime,
        FloorLimit? debitFloorLimit,
        FloorLimit? creditFloorLimit,
        EntrySummary? debitEntrySummary,
        EntrySummary? creditEntrySummary,
        string? informationToAccountOwner,
        IReadOnlyList<RawTag> unknownTags)
    {
        TransactionReference = transactionReference;
        RelatedReference = relatedReference;
        Account = account;
        StatementNumber = statementNumber;
        SequenceNumber = sequenceNumber;
        OpeningBalance = openingBalance;
        ClosingBalance = closingBalance;
        ClosingAvailableBalance = closingAvailableBalance;
        ForwardAvailableBalances = forwardAvailableBalances;
        Lines = lines;
        IsIntraday = isIntraday;
        ReportDateTime = reportDateTime;
        DebitFloorLimit = debitFloorLimit;
        CreditFloorLimit = creditFloorLimit;
        DebitEntrySummary = debitEntrySummary;
        CreditEntrySummary = creditEntrySummary;
        InformationToAccountOwner = informationToAccountOwner;
        UnknownTags = unknownTags;
    }

    /// <summary>The transaction reference number (:20:).</summary>
    public string TransactionReference { get; }

    /// <summary>The related reference (:21:), or null when absent.</summary>
    public string? RelatedReference { get; }

    /// <summary>The account identification (:25: or :25P:, first line). Empty when the tag was missing.</summary>
    public string Account { get; }

    /// <summary>The statement number from :28C: (or :28:). Empty when the tag was missing.</summary>
    public string StatementNumber { get; }

    /// <summary>The sequence number from :28C: after the slash, or null when absent.</summary>
    public string? SequenceNumber { get; }

    /// <summary>The opening balance (:60F: or :60M:), or null when absent (common in MT942).</summary>
    public Balance? OpeningBalance { get; }

    /// <summary>The closing booked balance (:62F: or :62M:), or null when absent (MT942).</summary>
    public Balance? ClosingBalance { get; }

    /// <summary>The closing available balance (:64:), or null when absent.</summary>
    public Balance? ClosingAvailableBalance { get; }

    /// <summary>The forward available balances (:65:), in file order. :65: is repeatable.</summary>
    public IReadOnlyList<Balance> ForwardAvailableBalances { get; }

    /// <summary>The statement lines (:61:), in file order, each with its attached :86: when present.</summary>
    public IReadOnlyList<StatementLine> Lines { get; }

    /// <summary>
    /// True when this is an MT942 interim report: it carries any of :13D:, :34F:, :90D:, :90C:,
    /// or it has neither opening nor closing booked balance.
    /// </summary>
    public bool IsIntraday { get; }

    /// <summary>The MT942 date/time indication (:13D:), or null when absent.</summary>
    public DateTimeOffset? ReportDateTime { get; }

    /// <summary>The MT942 debit floor limit (:34F:), or null when absent.</summary>
    public FloorLimit? DebitFloorLimit { get; }

    /// <summary>The MT942 credit floor limit (:34F: with C mark, or the single unmarked :34F:), or null.</summary>
    public FloorLimit? CreditFloorLimit { get; }

    /// <summary>The MT942 number and sum of debit entries (:90D:), or null when absent.</summary>
    public EntrySummary? DebitEntrySummary { get; }

    /// <summary>The MT942 number and sum of credit entries (:90C:), or null when absent.</summary>
    public EntrySummary? CreditEntrySummary { get; }

    /// <summary>The statement-level :86: (after the closing balance), or null when absent.</summary>
    public string? InformationToAccountOwner { get; }

    /// <summary>Tags the parser did not recognize, preserved verbatim, in file order.</summary>
    public IReadOnlyList<RawTag> UnknownTags { get; }

    /// <summary>The sum of the amounts of all debit-directed lines (D and RC). Non-negative.</summary>
    public decimal TotalDebits
    {
        get
        {
            var total = 0m;
            foreach (var line in Lines)
            {
                if (line.SignedAmount < 0)
                {
                    total += line.Amount;
                }
            }

            return total;
        }
    }

    /// <summary>The sum of the amounts of all credit-directed lines (C and RD). Non-negative.</summary>
    public decimal TotalCredits
    {
        get
        {
            var total = 0m;
            foreach (var line in Lines)
            {
                if (line.SignedAmount >= 0)
                {
                    total += line.Amount;
                }
            }

            return total;
        }
    }
}
