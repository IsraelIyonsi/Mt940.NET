using System.Text;

namespace Mt940.Internal;

internal static class SwiftText
{
    internal const char ByteOrderMark = (char)0xFEFF;

    private const string TextBlockOpener = "{4:";
    private const string BlockTerminator = "-}";

    internal static string NormalizeLineEndings(string text)
    {
        if (!text.Contains('\r'))
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (current == '\r' && (i + 1 == text.Length || text[i + 1] != '\n'))
            {
                builder.Append('\n');
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }

    internal static string FirstLine(string value)
    {
        var newlineIndex = value.IndexOf('\n');
        return newlineIndex < 0 ? value : value[..newlineIndex];
    }

    internal static List<(string Body, int BaseLineNumber)> ExtractBodies(string text)
    {
        var bodies = new List<(string, int)>();
        var searchFrom = text.IndexOf(TextBlockOpener, StringComparison.Ordinal);
        if (searchFrom < 0)
        {
            bodies.Add((text, 1));
            return bodies;
        }

        var lineCursorPosition = 0;
        var lineCursorLine = 1;

        int LineNumberAt(int position)
        {
            for (; lineCursorPosition < position; lineCursorPosition++)
            {
                if (text[lineCursorPosition] == '\n')
                {
                    lineCursorLine++;
                }
            }

            return lineCursorLine;
        }

        while (searchFrom >= 0)
        {
            var bodyStart = searchFrom + TextBlockOpener.Length;
            var terminator = text.IndexOf(BlockTerminator, bodyStart, StringComparison.Ordinal);
            var bodyEnd = terminator < 0 ? text.Length : terminator;
            bodies.Add((text[bodyStart..bodyEnd], LineNumberAt(bodyStart)));
            searchFrom = terminator < 0
                ? -1
                : text.IndexOf(TextBlockOpener, terminator + BlockTerminator.Length, StringComparison.Ordinal);
        }

        return bodies;
    }
}
