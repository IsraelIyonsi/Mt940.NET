# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-08-07

### Fixed

- Balance currency consistency now compares the full 3-character ISO 4217 code instead of only the first two characters. Genuinely distinct currencies that share a prefix, such as `CHF`/`CHE`, `USD`/`USN`, and `CNY`/`CNH`, previously reconciled silently with no warning because only two of the three characters were checked; they are now flagged and the amount check is skipped, as an opening/closing currency mismatch should be. The `:61:` statement-line funds code (the third character of the currency code, a separate per-line field) is unchanged.

## [0.1.0] - 2026-08-04

### Added

- `Mt940Parser.Parse`, `TryParse`, and `ParseAsync(Stream)` for SWIFT MT940 customer statements and MT942 interim transaction reports: bare tag streams or `{1:}{2:}{3:}{4:...-}` block-wrapped messages parsed in linear time, multiple statements per file, CRLF, LF, or lone-CR line endings, byte order mark tolerance on both paths, and UTF-8 decoding with a zero-dependency Windows-1252 fallback (euro sign and typographic quotes included).
- Full `:61:` statement line decomposition: value date, optional entry date resolved to the year nearest the value date (documented December/January rollover), `C`/`D`/`RC`/`RD` marks with reversal-aware `SignedAmount`, funds code, `decimal` amount, transaction type code, customer reference, `//` bank reference, and next-line supplementary details.
- Balance fields as typed records (`:60F:`/`:60M:`, `:62F:`/`:62M:`, `:64:`, repeatable `:65:`) with mark, date, ISO 4217 currency, `decimal` amount, signed amount, and intermediate flag.
- MT942 intraday fields: `:13D:` date/time indication with UTC offset, `:34F:` debit/credit floor limits (a single unmarked limit applies to both sides), `:90D:`/`:90C:` entry summaries, and `IsIntraday` detection.
- Multiline `:86:` information preserved raw with a strategy interface (`IInformationParser`) for structured sub-fields: `RawInformationParser` default plus `SlashDelimitedInformationParser` for the SEPA-style `/TAG/value` convention with a configurable sub-tag set. Consecutive `:86:` fields for one `:61:` (the ING repeat-`:86:` dialect) append to the line's information with a per-statement warning.
- Fail-loud `ParseReport`: unknown tags kept verbatim on `UnknownTags`, stray lines and missing mandatory tags surfaced as warnings, duplicate single-occurrence tags (including the MT942 set `:13D:`, `:34F:`, `:90D:`, `:90C:`) keep the first occurrence and warn, orphan `:86:` fields in the transaction region warn, ignored continuation lines on single-line tags warn, supplementary details beginning with `//` are flagged as a possibly wrapped bank reference, and a per-statement balance reconciliation check (`opening + signed lines == closing`) with configurable `Warn`/`Throw` behavior via `Mt940Options` that warns and stands down when opening and closing balance currencies disagree in their first two characters.
- Typed `Mt940ParseException : FormatException` carrying the line number and tag; `TryParse` structurally never throws: any failure yields false (fuzz-tested against 500+ deterministic mutations). Digit fields accept ASCII digits only, so Arabic-Indic digits fail with an honest invalid-format error instead of a misleading out-of-range one.
- Amount parsing regression coverage for the abandoned incumbents' bug: `1234,`, bare `1234`, and `,50` all parse exactly; invariant-culture parsing verified under de-DE, tr-TR, fr-FR, and ar-SA.
- `TotalDebits`/`TotalCredits` per statement for cross-checking against `:90D:`/`:90C:` and external books.
- `decimal` in every money path; zero runtime dependencies; SourceLink (GitHub), deterministic CI builds, and `.snupkg` symbol packages.
