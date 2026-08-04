# Mt940.NET

SWIFT MT940 and MT942 bank statement parser for .NET. End-of-day customer statements and intraday transaction reports, parsed into typed statements, balances, and fully decomposed statement lines. Zero external dependencies.

ISO 20022 was supposed to retire this format years ago. In practice, MT940 is still what corporates and fintechs actually receive from banks across Europe, Africa, and the Middle East: the nightly file on the SFTP server, the export behind the "download statement" button, the input every treasury and reconciliation system has to accept. The .NET ecosystem's existing parsers have been abandoned for years, one of them with an amount-parsing bug open since 2020. This library is a maintained replacement: strict about money, tolerant about the dialect noise banks produce, and loud about anything it cannot account for.

What it gives you:

- MT940 statements and MT942 intraday reports, multiple statements per file, bare tag streams or `{1:}{2:}{4:...-}` block-wrapped messages, CRLF or LF
- Full `:61:` decomposition: value date, entry date with documented year-rollover resolution, `C`/`D`/`RC`/`RD` marks, funds code, amount, transaction type, customer and bank references, supplementary details
- `decimal` for every amount, parsed invariantly: `1234,56`, `1234,`, `,50`, and the bare `1234` that broke the abandoned incumbents all parse exactly
- Multiline `:86:` preserved raw, plus a pluggable strategy for structured sub-fields with a built-in SEPA-style `/TAG/value` implementation
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

`Parse` throws a typed `Mt940ParseException` (with line number and tag) on structurally invalid input. `TryParse` never throws, whatever you feed it. `ParseAsync(Stream)` reads UTF-8 and falls back to Latin-1, which covers the encodings banks actually emit.

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
- Unreconciled balances warn or throw, per `BalanceMismatchBehavior`
- Structurally invalid fields throw `Mt940ParseException` with the line number and tag; `TryParse` converts exactly that failure into `false` and is fuzz-tested to never throw

## Limitations and roadmap

Honest constraints in 0.1:

- Structured `:86:` parsing ships one strategy, the slash-delimited SEPA convention. Fixed-position dialects (the German GVC/geschaeftsvorfallcode layout, `?20?21` sub-fields, and friends) need a custom `IInformationParser` for now; the raw text is always preserved.
- MT942 support covers the fields banks actually send (`:13D:`, `:34F:`, `:90D:`/`:90C:`, `:61:`/`:86:`); exotic optional fields land on `UnknownTags` rather than typed properties.
- There is no writer yet; this release parses only.
- Two-digit years pivot at 80: `80`-`99` mean 1980-1999, `00`-`79` mean 2000-2079.

Roadmap: an MT940 writer, a camt.053 sibling package, more built-in `:86:` dialect parsers (German GVC first), and streaming parse for very large files.

## License

MIT. See [LICENSE](LICENSE).
