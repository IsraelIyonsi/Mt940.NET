namespace Mt940;

/// <summary>An MT942 floor limit (:34F:): only transactions above this amount appear in the report.</summary>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Amount">The floor limit amount.</param>
public sealed record FloorLimit(string Currency, decimal Amount);
