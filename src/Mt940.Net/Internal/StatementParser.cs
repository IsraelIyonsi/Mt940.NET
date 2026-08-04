using System.Globalization;

namespace Mt940.Internal;

internal sealed class StatementParser
{
    private readonly int _statementIndex;
    private readonly Mt940Options _options;
    private readonly List<ParseWarning> _warnings;

    private string _transactionReference = string.Empty;
    private string? _relatedReference;
    private string? _account;
    private string? _statementNumber;
    private string? _sequenceNumber;
    private Balance? _openingBalance;
    private Balance? _closingBalance;
    private Balance? _closingAvailableBalance;
    private TagField? _closingBalanceField;
    private readonly List<Balance> _forwardAvailableBalances = [];
    private readonly List<StatementLineDraft> _drafts = [];
    private readonly List<RawTag> _unknownTags = [];
    private DateTimeOffset? _reportDateTime;
    private FloorLimit? _debitFloorLimit;
    private FloorLimit? _creditFloorLimit;
    private bool _floorLimitAppliesToBoth;
    private EntrySummary? _debitEntrySummary;
    private EntrySummary? _creditEntrySummary;
    private string? _informationToAccountOwner;
    private StatementLineDraft? _openLine;
    private bool _hasIntradayFields;

    private StatementParser(int statementIndex, Mt940Options options, List<ParseWarning> warnings)
    {
        _statementIndex = statementIndex;
        _options = options;
        _warnings = warnings;
    }

    internal static Mt940Statement Parse(
        IReadOnlyList<TagField> fields,
        int statementIndex,
        Mt940Options options,
        List<ParseWarning> warnings)
    {
        var parser = new StatementParser(statementIndex, options, warnings);
        foreach (var field in fields)
        {
            parser.Apply(field);
        }

        return parser.Build(fields[0]);
    }

    private void Apply(TagField field)
    {
        switch (field.Tag)
        {
            case TagNames.TransactionReference:
                _transactionReference = FirstLine(field.Value);
                break;
            case TagNames.RelatedReference:
                CloseLine();
                SetOnce(ref _relatedReference, FirstLine(field.Value), field);
                break;
            case TagNames.AccountIdentification:
            case TagNames.AccountIdentificationWithOwner:
                CloseLine();
                SetOnce(ref _account, FirstLine(field.Value), field);
                break;
            case TagNames.StatementNumber:
            case TagNames.StatementNumberShort:
                CloseLine();
                ApplyStatementNumber(field);
                break;
            case TagNames.OpeningBalanceFinal:
            case TagNames.OpeningBalanceIntermediate:
                CloseLine();
                ApplyBalance(ref _openingBalance, field, field.Tag == TagNames.OpeningBalanceIntermediate);
                break;
            case TagNames.StatementLine:
                CloseLine();
                ApplyStatementLine(field);
                break;
            case TagNames.Information:
                ApplyInformation(field);
                break;
            case TagNames.ClosingBalanceFinal:
            case TagNames.ClosingBalanceIntermediate:
                CloseLine();
                if (ApplyBalance(ref _closingBalance, field, field.Tag == TagNames.ClosingBalanceIntermediate))
                {
                    _closingBalanceField = field;
                }

                break;
            case TagNames.ClosingAvailableBalance:
                CloseLine();
                ApplyBalance(ref _closingAvailableBalance, field, isIntermediate: false);
                break;
            case TagNames.ForwardAvailableBalance:
                CloseLine();
                _forwardAvailableBalances.Add(
                    FieldParsers.ParseBalance(field.Value, field.LineNumber, field.Tag, isIntermediate: false));
                break;
            case TagNames.DateTimeIndication:
                CloseLine();
                _hasIntradayFields = true;
                _reportDateTime ??= FieldParsers.ParseDateTimeIndication(field.Value, field.LineNumber, field.Tag);
                break;
            case TagNames.FloorLimit:
                CloseLine();
                _hasIntradayFields = true;
                ApplyFloorLimit(field);
                break;
            case TagNames.DebitEntrySummary:
                CloseLine();
                _hasIntradayFields = true;
                _debitEntrySummary ??= FieldParsers.ParseEntrySummary(field.Value, field.LineNumber, field.Tag);
                break;
            case TagNames.CreditEntrySummary:
                CloseLine();
                _hasIntradayFields = true;
                _creditEntrySummary ??= FieldParsers.ParseEntrySummary(field.Value, field.LineNumber, field.Tag);
                break;
            default:
                _unknownTags.Add(new RawTag(field.Tag, field.Value, field.LineNumber));
                Warn(field, $"Unknown tag :{field.Tag}: kept raw on {nameof(Mt940Statement.UnknownTags)}.");
                break;
        }
    }

    private void ApplyStatementNumber(TagField field)
    {
        if (_statementNumber is not null)
        {
            WarnDuplicate(field);
            return;
        }

        (_statementNumber, _sequenceNumber) =
            FieldParsers.ParseStatementNumber(field.Value, field.LineNumber, field.Tag);
    }

    private bool ApplyBalance(ref Balance? slot, TagField field, bool isIntermediate)
    {
        if (slot is not null)
        {
            WarnDuplicate(field);
            return false;
        }

        slot = FieldParsers.ParseBalance(field.Value, field.LineNumber, field.Tag, isIntermediate);
        return true;
    }

    private void ApplyStatementLine(TagField field)
    {
        var draft = FieldParsers.ParseStatementLine(field.Value, field.LineNumber);
        if (FieldParsers.ExceedsCustomerReferenceLength(draft.CustomerReference))
        {
            Warn(field, $"Customer reference \"{draft.CustomerReference}\" exceeds 16 characters; kept as sent.");
        }

        _drafts.Add(draft);
        _openLine = draft;
    }

    private void ApplyInformation(TagField field)
    {
        if (_openLine is not null)
        {
            _openLine.Information = field.Value;
            _openLine = null;
            return;
        }

        _informationToAccountOwner = _informationToAccountOwner is null
            ? field.Value
            : _informationToAccountOwner + "\n" + field.Value;
    }

    private void ApplyFloorLimit(TagField field)
    {
        var (limit, mark) = FieldParsers.ParseFloorLimit(field.Value, field.LineNumber, field.Tag);
        switch (mark)
        {
            case DebitCreditMark.Credit:
                _creditFloorLimit = limit;
                break;
            case DebitCreditMark.Debit:
                _debitFloorLimit = limit;
                break;
            default:
                if (_debitFloorLimit is null)
                {
                    _debitFloorLimit = limit;
                    _floorLimitAppliesToBoth = true;
                }
                else
                {
                    _creditFloorLimit = limit;
                }

                break;
        }
    }

    private void CloseLine() => _openLine = null;

    private void SetOnce(ref string? slot, string value, TagField field)
    {
        if (slot is not null)
        {
            WarnDuplicate(field);
            return;
        }

        slot = value;
    }

    private void WarnDuplicate(TagField field) =>
        Warn(field, $"Duplicate tag :{field.Tag}:; the first occurrence was kept and this one ignored.");

    private void Warn(TagField field, string message) =>
        _warnings.Add(new ParseWarning(_statementIndex, field.LineNumber, field.Tag, message));

    private Mt940Statement Build(TagField openingField)
    {
        if (_floorLimitAppliesToBoth && _creditFloorLimit is null)
        {
            _creditFloorLimit = _debitFloorLimit;
        }

        WarnOnMissingMandatoryTags(openingField);
        CheckBalanceReconciliation();

        var isIntraday = _hasIntradayFields || (_openingBalance is null && _closingBalance is null);
        var lines = _drafts
            .Select(draft => new StatementLine(
                draft.ValueDate,
                draft.EntryDate,
                draft.Mark,
                draft.FundsCode,
                draft.Amount,
                draft.TransactionType,
                draft.CustomerReference,
                draft.BankReference,
                draft.SupplementaryDetails,
                draft.Information,
                ParseStructuredInformation(draft)))
            .ToArray();

        return new Mt940Statement(
            _transactionReference,
            _relatedReference,
            _account ?? string.Empty,
            _statementNumber ?? string.Empty,
            _sequenceNumber,
            _openingBalance,
            _closingBalance,
            _closingAvailableBalance,
            _forwardAvailableBalances.AsReadOnly(),
            lines,
            isIntraday,
            _reportDateTime,
            _debitFloorLimit,
            _creditFloorLimit,
            _debitEntrySummary,
            _creditEntrySummary,
            _informationToAccountOwner,
            _unknownTags.AsReadOnly());
    }

    private void WarnOnMissingMandatoryTags(TagField openingField)
    {
        if (_account is null)
        {
            _warnings.Add(new ParseWarning(
                _statementIndex, openingField.LineNumber, TagNames.AccountIdentification,
                "Statement has no :25: account identification."));
        }

        if (_statementNumber is null)
        {
            _warnings.Add(new ParseWarning(
                _statementIndex, openingField.LineNumber, TagNames.StatementNumber,
                "Statement has no :28C: statement number."));
        }

        if (_hasIntradayFields)
        {
            return;
        }

        if (_openingBalance is null && _closingBalance is not null)
        {
            _warnings.Add(new ParseWarning(
                _statementIndex, openingField.LineNumber, TagNames.OpeningBalanceFinal,
                "Statement has a closing balance but no opening balance; reconciliation skipped."));
        }
        else if (_openingBalance is not null && _closingBalance is null)
        {
            _warnings.Add(new ParseWarning(
                _statementIndex, openingField.LineNumber, TagNames.ClosingBalanceFinal,
                "Statement has an opening balance but no closing balance; reconciliation skipped."));
        }
    }

    private void CheckBalanceReconciliation()
    {
        if (_openingBalance is null || _closingBalance is null || _closingBalanceField is null)
        {
            return;
        }

        var sumOfLines = 0m;
        foreach (var draft in _drafts)
        {
            sumOfLines += draft.SignedAmount;
        }

        var expected = _openingBalance.SignedAmount + sumOfLines;
        var actual = _closingBalance.SignedAmount;
        if (expected == actual)
        {
            return;
        }

        var field = _closingBalanceField.Value;
        var message = string.Create(
            CultureInfo.InvariantCulture,
            $"Balances do not reconcile: opening {_openingBalance.SignedAmount} plus signed lines {sumOfLines} gives {expected}, but the closing balance is {actual}.");
        if (_options.BalanceMismatchBehavior == BalanceMismatchBehavior.Throw)
        {
            throw new Mt940ParseException(message, field.LineNumber, field.Tag);
        }

        Warn(field, message);
    }

    private IReadOnlyDictionary<string, string> ParseStructuredInformation(StatementLineDraft draft)
    {
        if (draft.Information is null)
        {
            return RawInformationParser.Empty;
        }

        try
        {
            return _options.InformationParser.Parse(draft.Information) ?? RawInformationParser.Empty;
        }
        catch (Exception exception) when (exception is not Mt940ParseException)
        {
            _warnings.Add(new ParseWarning(
                _statementIndex, draft.LineNumber, TagNames.Information,
                $"The configured information parser threw {exception.GetType().Name}: {exception.Message}. " +
                "Structured information left empty."));
            return RawInformationParser.Empty;
        }
    }

    private static string FirstLine(string value)
    {
        var newlineIndex = value.IndexOf('\n');
        return newlineIndex < 0 ? value : value[..newlineIndex];
    }
}
