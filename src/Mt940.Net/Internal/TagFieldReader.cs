using System.Text;
using System.Text.RegularExpressions;

namespace Mt940.Internal;

internal static partial class TagFieldReader
{
    private const string MessageTerminator = "-";

    [GeneratedRegex(@"^:([0-9]{2}[A-Z]?|[A-Z]{2}):(.*)$")]
    private static partial Regex TagLinePattern();

    internal static void Read(string body, int baseLineNumber, List<TagField> fields, List<ParseWarning> warnings)
    {
        var lines = body.Split('\n');
        string? openTag = null;
        StringBuilder? openValue = null;
        var openLineNumber = 0;

        void CloseOpenField()
        {
            if (openTag is not null)
            {
                fields.Add(new TagField(openTag, openValue!.ToString(), openLineNumber));
                openTag = null;
                openValue = null;
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = baseLineNumber + i;
            var line = lines[i].TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            if (line == MessageTerminator)
            {
                CloseOpenField();
                continue;
            }

            var match = TagLinePattern().Match(line);
            if (match.Success)
            {
                CloseOpenField();
                openTag = match.Groups[1].Value;
                openValue = new StringBuilder(match.Groups[2].Value);
                openLineNumber = lineNumber;
                continue;
            }

            if (openTag is not null)
            {
                openValue!.Append('\n').Append(line);
                continue;
            }

            warnings.Add(new ParseWarning(
                null, lineNumber, string.Empty,
                $"Line is neither a tag nor a continuation of one; ignored: \"{line}\"."));
        }

        CloseOpenField();
    }
}
