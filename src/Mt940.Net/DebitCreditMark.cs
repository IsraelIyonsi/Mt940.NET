namespace Mt940;

/// <summary>
/// The debit/credit mark of a balance or statement line. Balances carry <see cref="Credit"/> or
/// <see cref="Debit"/>; statement lines may additionally carry the reversal marks.
/// </summary>
public enum DebitCreditMark
{
    /// <summary>C: a credit to the account.</summary>
    Credit,

    /// <summary>D: a debit to the account.</summary>
    Debit,

    /// <summary>RC: reversal of a previous credit. The money movement is debit-directed.</summary>
    ReversalCredit,

    /// <summary>RD: reversal of a previous debit. The money movement is credit-directed.</summary>
    ReversalDebit,
}
