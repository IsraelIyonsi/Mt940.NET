namespace Mt940.Internal;

internal static class Mt940Engine
{
    internal static Mt940File Parse(string text, Mt940Options options)
    {
        var warnings = new List<ParseWarning>();
        var fields = new List<TagField>();
        foreach (var (body, baseLineNumber) in SwiftText.ExtractBodies(text))
        {
            TagFieldReader.Read(body, baseLineNumber, fields, warnings);
        }

        var statements = new List<Mt940Statement>();
        var index = 0;
        while (index < fields.Count && fields[index].Tag != TagNames.TransactionReference)
        {
            var stray = fields[index];
            warnings.Add(new ParseWarning(
                null, stray.LineNumber, stray.Tag,
                $"Tag :{stray.Tag}: appears before the first :20:; it belongs to no statement and was ignored."));
            index++;
        }

        while (index < fields.Count)
        {
            var start = index;
            index++;
            while (index < fields.Count && fields[index].Tag != TagNames.TransactionReference)
            {
                index++;
            }

            var slice = fields.GetRange(start, index - start);
            statements.Add(StatementParser.Parse(slice, statements.Count, options, warnings));
        }

        return new Mt940File(statements.AsReadOnly(), new ParseReport(warnings.AsReadOnly()));
    }
}
