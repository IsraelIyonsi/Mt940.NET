namespace Mt940;

/// <summary>An MT942 number-and-sum-of-entries field (:90D: or :90C:).</summary>
/// <param name="Count">The number of entries.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Amount">The sum of the entry amounts.</param>
public sealed record EntrySummary(int Count, string Currency, decimal Amount);
