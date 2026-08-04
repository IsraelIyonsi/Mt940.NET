namespace Mt940.Internal;

internal sealed class StatementLineDraft
{
    internal required int LineNumber { get; init; }

    internal required DateOnly ValueDate { get; init; }

    internal required DateOnly? EntryDate { get; init; }

    internal required DebitCreditMark Mark { get; init; }

    internal required char? FundsCode { get; init; }

    internal required decimal Amount { get; init; }

    internal required string TransactionType { get; init; }

    internal required string CustomerReference { get; init; }

    internal required string? BankReference { get; init; }

    internal required string? SupplementaryDetails { get; init; }

    internal string? Information { get; set; }

    internal decimal SignedAmount =>
        Mark is DebitCreditMark.Credit or DebitCreditMark.ReversalDebit ? Amount : -Amount;
}
