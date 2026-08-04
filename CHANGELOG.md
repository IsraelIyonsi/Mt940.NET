# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-04

### Added

- `Mt940Parser.Parse`, `TryParse`, and `ParseAsync(Stream)` for SWIFT MT940 customer statements and MT942 interim transaction reports: bare tag streams or `{1:}{2:}{3:}{4:...-}` block-wrapped messages, multiple statements per file, CRLF or LF line endings, UTF-8 with Latin-1 fallback and BOM tolerance on the stream path.
- Full `:61:` statement line decomposition: value date, optional entry date resolved to the year nearest the value date (documented December/January rollover), `C`/`D`/`RC`/`RD` marks with reversal-aware `SignedAmount`, funds code, `decimal` amount, transaction type code, customer reference, `//` bank reference, and next-line supplementary details.
- Balance fields as typed records (`:60F:`/`:60M:`, `:62F:`/`:62M:`, `:64:`, repeatable `:65:`) with mark, date, ISO 4217 currency, `decimal` amount, signed amount, and intermediate flag.
- MT942 intraday fields: `:13D:` date/time indication with UTC offset, `:34F:` debit/credit floor limits (a single unmarked limit applies to both sides), `:90D:`/`:90C:` entry summaries, and `IsIntraday` detection.
- Multiline `:86:` information preserved raw with a strategy interface (`IInformationParser`) for structured sub-fields: `RawInformationParser` default plus `SlashDelimitedInformationParser` for the SEPA-style `/TAG/value` convention with a configurable sub-tag set.
- Fail-loud `ParseReport`: unknown tags kept verbatim on `UnknownTags`, stray lines and missing mandatory tags surfaced as warnings, and a per-statement balance reconciliation check (`opening + signed lines == closing`) with configurable `Warn`/`Throw` behavior via `Mt940Options`.
- Typed `Mt940ParseException : FormatException` carrying the line number and tag; `TryParse` never throws (fuzz-tested against 500+ deterministic mutations).
- Amount parsing regression coverage for the abandoned incumbents' bug: `1234,`, bare `1234`, and `,50` all parse exactly; invariant-culture parsing verified under de-DE, tr-TR, fr-FR, and ar-SA.
- `TotalDebits`/`TotalCredits` per statement for cross-checking against `:90D:`/`:90C:` and external books.
- `decimal` in every money path; zero runtime dependencies; SourceLink (GitHub), deterministic CI builds, and `.snupkg` symbol packages.
