# Mt940.NET

SWIFT MT940 and MT942 bank statement parser for .NET. End-of-day customer statements and intraday transaction reports, parsed into typed statements, balances, and fully decomposed statement lines. Zero external dependencies.

ISO 20022 was supposed to retire this format years ago. In practice, MT940 is still what corporates and fintechs actually receive from banks across Europe, Africa, and the Middle East: the nightly file on the SFTP server, the export behind the "download statement" button, the input every treasury and reconciliation system has to accept. The .NET ecosystem's existing parsers have been abandoned for years, one of them with an amount-parsing bug open since 2020. This library is a maintained replacement: strict about money, tolerant about the dialect noise banks produce, and loud about anything it cannot account for.

What it gives you:

- MT940 statements and MT942 intraday reports, multiple statements per file, bare tag streams or `{1:}{2:}{4:...-}` block-wrapped messages, CRLF, LF, or lone-CR line endings (lone CR is normalized before parsing)
- Full `:61:` decomposition: value date, entry date with documented year-rollover resolution, `C`/`D`/`RC`/`RD` marks, funds code, amount, transaction type, customer and bank references, supplementary details
- `decimal` for every amount, parsed invariantly: `1234,56`, `1234,`, `,50`, and the bare `1234` that broke the abandoned incumbents all parse exactly
- Multiline `:86:` preserved raw, plus a pluggable strategy for structured sub-fields with two built-in implementations: the SEPA-style `/TAG/value` convention and the German GVC `?NN` layout
- A fail-loud parse report: unknown tags kept verbatim, stray lines surfaced, and a balance reconciliation check (opening + signed lines = closing) that warns or throws, your choice

## Install

```
dotnet add package Mt940.Net
```

## Quickstart

```csharp
using System;
using System.IO;
using Mt940;

var file = Mt940Parser.Parse(File.ReadAllText("statement.sta"));

foreach (var statement in file.Statements)
{
    Console.WriteLine($"{statement.Account} #{statement.StatementNumber}");
    Console.WriteLine($"Opening {statement.OpeningBalance?.SignedAmount} {statement.OpeningBalance?.Currency}");

    foreach (var line in statement.Lines)
    {
        Console.WriteLine(
            $"  {line.ValueDate:yyyy-MM-dd} {line.SignedAmount,12} {line.TransactionType} " +
            $"{line.CustomerReference} {line.Information}");
    }

    Console.WriteLine($"Closing {statement.ClosingBalance?.SignedAmount} {statement.ClosingBalance?.Currency}");
}

foreach (var warning in file.Report.Warnings)
{
    Console.WriteLine($"warning: line {warning.LineNumber} :{warning.Tag}: {warning.Message}");
}
```

`Parse` throws a typed `Mt940ParseException` (with line number and tag) on structurally invalid input. `TryParse` never throws, whatever you feed it. `ParseAsync(Stream)` reads UTF-8 and falls back to Windows-1252 (a superset of printable Latin-1 that adds the euro sign and the typographic quotes banks actually emit).

## Anatomy of a :61: line

The statement line is the hard part of the format and the reason half-finished parsers fail on real files. Every subfield is parsed into a typed property:

```
:61:2601021231D1234,56NTRFINV-2026-071//BANKREF123
    |     |   ||     |   |            |
    |     |   ||     |   |            +-- bank reference        -> BankReference ("BANKREF123")
    |     |   ||     |   +--------------- customer reference    -> CustomerReference ("INV-2026-071")
    |     |   ||     +------------------- transaction type      -> TransactionType ("NTRF")
    |     |   |+------------------------- amount, comma decimal -> Amount (1234.56m)
    |     |   +-------------------------- debit/credit mark     -> Mark (Debit; also C, RC, RD)
    |     +------------------------------ entry date MMDD       -> EntryDate (2025-12-31, see below)
    +------------------------------------ value date YYMMDD     -> ValueDate (2026-01-02)
SUPPLEMENTARY DETAILS ON THE NEXT LINE    -> SupplementaryDetails
```

Details that matter:

- **Entry date year rollover.** The entry date has no year. It resolves to the candidate year (value date's year, the one before, or the one after) that lands nearest the value date, so a 2 January statement with entry date `1231` books to 31 December of the previous year, and vice versa.
- **Reversal marks.** `SignedAmount` is positive for `C` and `RD` (credit-directed movements), negative for `D` and `RC`. `RD` is the reversal of a debit: money coming back.
- **Funds code.** A letter between the mark and the amount (`CR100,00` is a credit of 100 with funds code `R`, not a reversal) is kept on `FundsCode`.
- **Amounts.** `decimal` end to end, comma decimal separator, parsed under the invariant culture. `1234,`, `,50`, and separator-free `1234` all parse; nothing goes through `double`, ever.

## Balance reconciliation

A statement whose lines do not explain the movement from opening to closing balance is a statement you should not trust silently. The parser checks `opening + sum of signed lines == closing` on every statement that has both balances:

```csharp
using Mt940;

var options = new Mt940Options
{
    BalanceMismatchBehavior = BalanceMismatchBehavior.Throw, // default is Warn
};

var file = Mt940Parser.Parse(text, options);
```

With the default `Warn`, the mismatch (including expected and actual amounts) lands on `file.Report.Warnings` and parsing continues. `Mt940Statement` also exposes `TotalDebits` and `TotalCredits` so you can cross-check against `:90D:`/`:90C:` or your own books.

## Structured :86: information

The raw `:86:` text is always on `StatementLine.Information`, continuation lines joined with `\n`. Many European banks additionally structure it with `/TAG/value` code words. Opt in to the built-in parser:

```csharp
using System;
using System.IO;
using Mt940;

var options = new Mt940Options
{
    InformationParser = SlashDelimitedInformationParser.Default,
};

var file = Mt940Parser.Parse(File.ReadAllText("statement.sta"), options);

foreach (var line in file.Statements[0].Lines)
{
    if (line.StructuredInformation.TryGetValue("EREF", out var endToEndReference))
    {
        Console.WriteLine($"{line.SignedAmount}: {endToEndReference}");
    }
}
```

`/EREF/E2E-42/MARF/MND-001/BENM//NAME/ACME BV/REMI/INVOICE 42` becomes `EREF=E2E-42`, `MARF=MND-001`, `BENM=` (empty group marker), `NAME=ACME BV`, `REMI=INVOICE 42`. Wrapped `:86:` lines are joined before scanning because banks split the 65-character lines mid-token.

Bank dialects vary; that is the point of the strategy interface. Pass your own sub-tag set to `new SlashDelimitedInformationParser(...)`, or implement `IInformationParser` for dialects that are not slash-delimited at all. Nothing is lost either way: the raw text stays available.

### German GVC (`?NN`) dialect

German and Austrian banks (DK / DFUE-Abkommen Anlage 3) structure `:86:` differently: a leading 3-digit GVC (Geschaeftsvorfallcode, the booking code) followed by sub-fields introduced by `?NN` markers. Opt in to `GermanGvcInformationParser.Default` instead:

```csharp
var options = new Mt940Options
{
    InformationParser = GermanGvcInformationParser.Default,
};
```

`166?00SEPA-UEBERWEISUNG?20EREF+INV-2024-0042?21SVWZ+Rechnung 42?22 Miete Mai?30GENODEF1S02?31DE02500105170137075030?32Max Mustermann?33GmbH?34999` becomes:

| Key | Value |
| --- | --- |
| `Gvc` | `166` |
| `BookingText` | `SEPA-UEBERWEISUNG` |
| `Purpose` | `EREF+INV-2024-0042SVWZ+Rechnung 42 Miete Mai` |
| `Bic` | `GENODEF1S02` |
| `Iban` | `DE02500105170137075030` |
| `Name` | `Max MustermannGmbH` |
| `TextKey` | `999` |

The purpose sub-fields (`?20`-`?29` and `?60`-`?63`) and the counterparty-name sub-fields (`?32`-`?33`) are concatenated in the order they appear, with **no separator**, because the bank has already placed any needed spacing inside each part. `?00` is the booking text, `?10` the primanota, `?30` the counterparty BIC, `?31` the counterparty IBAN, and `?34` the text key; any other `?NN` is preserved under the key `?NN` (for example `?40`). Wrapped lines are joined before scanning, and a `?` not followed by two digits is kept as literal content rather than treated as a marker. The dictionary keys are also exposed as `GermanGvcInformationParser.GvcKey`, `.PurposeKey`, and so on, so you never have to hard-code them.

## Feeding Reconcile.Net

Mt940.NET pairs with [Reconcile.Net](https://github.com/IsraelIyonsi/Reconcile.NET): this library turns the bank file into typed lines, that one matches the lines against your ledger. This compiles as pasted with both packages installed:

```csharp
using System;
using System.IO;
using System.Linq;
using Mt940;
using Reconcile.Net;

var bankLines = Mt940Parser.Parse(File.ReadAllText("statement.sta"))
    .Statements
    .SelectMany(statement => statement.Lines)
    .ToArray();

var ledger = new[]
{
    new LedgerRow("INV-2026-071", 350.25m, new DateTime(2026, 8, 1)),
    new LedgerRow("INV-2026-072", 1_200.00m, new DateTime(2026, 8, 2)),
};

var result = Reconciliation
    .Between(ledger, bankLines)
    .ThenMatchOn(pass => pass
        .Key(l => l.Reference, b => b.CustomerReference)
        .Amount(l => l.Amount, b => Math.Abs(b.SignedAmount), tolerance: 0.01m)
        .Date(l => l.PostedAt, b => b.ValueDate.ToDateTime(TimeOnly.MinValue),
            window: TimeSpan.FromDays(2)))
    .Run();

Console.WriteLine($"Matched: {result.Matched.Count}");
Console.WriteLine($"Unexplained bank lines: {result.UnmatchedRight.Count}");

public sealed record LedgerRow(string Reference, decimal Amount, DateTime PostedAt);
```

## MT942 intraday reports

A statement with `:13D:`, `:34F:`, or `:90D:`/`:90C:` (or with no booked balances at all) is flagged `IsIntraday`. The MT942-specific fields are typed: `ReportDateTime` (with UTC offset), `DebitFloorLimit`/`CreditFloorLimit` (a single unmarked `:34F:` applies to both sides, per the spec), and `DebitEntrySummary`/`CreditEntrySummary` with entry counts and sums.

## Fail-loud philosophy

Nothing is silently dropped:

- Unknown tags are preserved verbatim on `Mt940Statement.UnknownTags` and each raises a warning
- Lines that are neither a tag nor a continuation raise a warning
- Missing mandatory tags (`:25:`, `:28C:`, a lone opening or closing balance) raise warnings
- Unreconciled balances warn or throw, per `BalanceMismatchBehavior`; opening and closing balances in different currencies raise a warning and skip the amount check (the balance currency is compared in full as a 3-character ISO 4217 code, so genuinely distinct currencies that share a prefix, such as `CHF` and `CHE` or `USD` and `USN`, are flagged rather than silently reconciled). This is the atomic balance currency of `:60a:`/`:62a:`; it is unrelated to the `:61:` statement-line funds code, which is separately defined as the third character of the currency code and handled per line.
- Duplicate single-occurrence tags, including the MT942 set (`:13D:`, `:34F:`, `:90D:`, `:90C:`), keep the first occurrence and warn about the rest
- Consecutive `:86:` fields for one `:61:` (the repeat-`:86:` dialect ING and others emit) are appended to that line's information, and the dialect is flagged with one warning per statement
- A statement-level `:86:` that appears in the transaction region with no preceding `:61:` raises a warning instead of being absorbed silently
- Continuation lines on single-line tags (`:20:`, `:21:`, `:25:`, `:28C:`) warn when they are ignored, and supplementary details that begin with `//` are flagged as a possibly wrapped bank reference
- Structurally invalid fields throw `Mt940ParseException` with the line number and tag; `TryParse` never throws, structurally: it converts any failure into `false` and is fuzz-tested against hundreds of deterministic mutations

Two sharp edges worth knowing. A narrative continuation line that happens to begin with `:20:` legitimately starts a new statement; that is how SWIFT tag streams work, and it is why banks are required to escape colons at line starts. And `:28C:` is parsed strictly to its `5n[/5n]` format: statement or sequence numbers longer than five digits are rejected loudly rather than truncated.

## Limitations and roadmap

Honest constraints in 0.1:

- Structured `:86:` parsing ships two strategies: the slash-delimited SEPA convention and the German GVC `?NN` layout. Other dialects still need a custom `IInformationParser`; the raw text is always preserved.
- MT942 support covers the fields banks actually send (`:13D:`, `:34F:`, `:90D:`/`:90C:`, `:61:`/`:86:`); exotic optional fields land on `UnknownTags` rather than typed properties.
- There is no writer yet; this release parses only.
- Two-digit years pivot at 80: `80`-`99` mean 1980-1999, `00`-`79` mean 2000-2079.

Roadmap: an MT940 writer, a camt.053 sibling package, more built-in `:86:` dialect parsers, and streaming parse for very large files.

## License

MIT. See [LICENSE](LICENSE).
