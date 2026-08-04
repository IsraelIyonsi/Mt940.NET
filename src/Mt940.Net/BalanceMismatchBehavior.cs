namespace Mt940;

/// <summary>
/// What the parser does when a statement's balances do not reconcile, that is when
/// opening balance plus the sum of the signed statement line amounts differs from the closing balance.
/// </summary>
public enum BalanceMismatchBehavior
{
    /// <summary>Record a <see cref="ParseWarning"/> on the <see cref="ParseReport"/> and keep parsing. The default.</summary>
    Warn,

    /// <summary>Throw an <see cref="Mt940ParseException"/> pointing at the closing balance field.</summary>
    Throw,
}
