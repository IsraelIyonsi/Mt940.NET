namespace Mt940;

/// <summary>A balance field (:60a:, :62a:, :64:, :65:): mark, date, ISO 4217 currency, and amount.</summary>
/// <param name="Mark">Whether the balance is a credit or a debit balance.</param>
/// <param name="Date">The balance date.</param>
/// <param name="Currency">The ISO 4217 currency code, exactly as it appears in the field.</param>
/// <param name="Amount">The unsigned amount. Always non-negative.</param>
/// <param name="IsIntermediate">True when the field was :60M: or :62M: (an intermediate balance in a
/// multi-page statement) rather than :60F: or :62F:. Always false for :64: and :65:.</param>
public sealed record Balance(
    DebitCreditMark Mark,
    DateOnly Date,
    string Currency,
    decimal Amount,
    bool IsIntermediate)
{
    /// <summary>
    /// The amount signed by the mark: positive for a credit balance, negative for a debit balance.
    /// </summary>
    public decimal SignedAmount => Mark == DebitCreditMark.Debit ? -Amount : Amount;
}
