namespace Mt940.Internal;

internal static class SwiftText
{
    private const string TextBlockOpener = "{4:";
    private const string BlockTerminator = "-}";

    internal static List<(string Body, int BaseLineNumber)> ExtractBodies(string text)
    {
        var bodies = new List<(string, int)>();
        var searchFrom = text.IndexOf(TextBlockOpener, StringComparison.Ordinal);
        if (searchFrom < 0)
        {
            bodies.Add((text, 1));
            return bodies;
        }

        while (searchFrom >= 0)
        {
            var bodyStart = searchFrom + TextBlockOpener.Length;
            var terminator = text.IndexOf(BlockTerminator, bodyStart, StringComparison.Ordinal);
            var bodyEnd = terminator < 0 ? text.Length : terminator;
            bodies.Add((text[bodyStart..bodyEnd], LineNumberAt(text, bodyStart)));
            searchFrom = terminator < 0
                ? -1
                : text.IndexOf(TextBlockOpener, terminator + BlockTerminator.Length, StringComparison.Ordinal);
        }

        return bodies;
    }

    private static int LineNumberAt(string text, int position)
    {
        var line = 1;
        for (var i = 0; i < position && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
