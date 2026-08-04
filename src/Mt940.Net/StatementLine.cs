namespace Mt940;

/// <summary>One :61: statement line, with its attached :86: information when present.</summary>
public sealed class StatementLine
{
    internal StatementLine(
        DateOnly valueDate,
        DateOnly? entryDate,
        DebitCreditMark mark,
        char? fundsCode,
        decimal amount,
        string transactionType,
        string customerReference,
        string? bankReference,
        string? supplementaryDetails,
        string? information,
        IReadOnlyDictionary<string, string> structuredInformation)
    {
        ValueDate = valueDate;
        EntryDate = entryDate;
        Mark = mark;
        FundsCode = fundsCode;
        Amount = amount;
        TransactionType = transactionType;
        CustomerReference = customerReference;
        BankReference = bankReference;
        SupplementaryDetails = supplementaryDetails;
        Information = information;
        StructuredInformation = structuredInformation;
    }

    /// <summary>The value date (subfield 1, YYMMDD).</summary>
    public DateOnly ValueDate { get; }

    /// <summary>
    /// The entry (booking) date (subfield 2, MMDD), or null when absent. The year is resolved to the
    /// candidate nearest the value date, so a statement of 2 January with entry date 1231 books to
    /// 31 December of the previous year, and a 31 December statement with entry date 0102 books to
    /// 2 January of the next year.
    /// </summary>
    public DateOnly? EntryDate { get; }

    /// <summary>The debit/credit mark (subfield 3): C, D, RC, or RD.</summary>
    public DebitCreditMark Mark { get; }

    /// <summary>The funds code (subfield 4): the third character of the currency code, or null when absent.</summary>
    public char? FundsCode { get; }

    /// <summary>The unsigned amount (subfield 5). Always non-negative.</summary>
    public decimal Amount { get; }

    /// <summary>The transaction type identification code (subfield 6), for example "NTRF" or "S103".</summary>
    public string TransactionType { get; }

    /// <summary>The reference for the account owner (subfield 7). Empty when the bank sent none.</summary>
    public string CustomerReference { get; }

    /// <summary>The bank reference (subfield 8, after //), or null when absent.</summary>
    public string? BankReference { get; }

    /// <summary>The supplementary details (subfield 9, on the continuation line of :61:), or null.</summary>
    public string? SupplementaryDetails { get; }

    /// <summary>
    /// The raw :86: information to account owner attached to this line, with continuation lines
    /// joined by '\n', or null when the line had no :86:.
    /// </summary>
    public string? Information { get; }

    /// <summary>
    /// The :86: information parsed by the configured <see cref="IInformationParser"/> strategy.
    /// Empty with the default <see cref="RawInformationParser"/> or when the line had no :86:.
    /// </summary>
    public IReadOnlyDictionary<string, string> StructuredInformation { get; }

    /// <summary>
    /// The amount signed by the direction of the money movement: positive for C and RD
    /// (credit-directed), negative for D and RC (debit-directed).
    /// </summary>
    public decimal SignedAmount =>
        Mark is DebitCreditMark.Credit or DebitCreditMark.ReversalDebit ? Amount : -Amount;
}
