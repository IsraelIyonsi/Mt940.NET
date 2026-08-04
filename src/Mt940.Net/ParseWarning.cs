namespace Mt940;

/// <summary>A non-fatal problem found while parsing. Nothing is ever silently dropped.</summary>
/// <param name="StatementIndex">The 0-based index of the statement the warning belongs to within
/// <see cref="Mt940File.Statements"/>, or null when the problem occurred outside any statement.</param>
/// <param name="LineNumber">The 1-based line number in the input text, or 0 when no single line applies.</param>
/// <param name="Tag">The tag the warning relates to, without colons; empty when no tag applies.</param>
/// <param name="Message">A human-readable description of the problem.</param>
public sealed record ParseWarning(int? StatementIndex, int LineNumber, string Tag, string Message);
