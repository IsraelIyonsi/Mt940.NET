using System.Text;
using Mt940.Tests.Support;

namespace Mt940.Tests.Fuzz;

/// <summary>
/// TryParse must never throw, whatever the input. All mutations are deterministic:
/// fixed seeds, fixed fixture set, so a failure reproduces exactly.
/// </summary>
public sealed class FuzzTests
{
    private const int RandomSeed = 20260804;
    private const int ShufflesPerFixture = 15;
    private const int GarbageMutationsPerFixture = 25;
    private const int MinimumTotalCases = 500;

    private static readonly char[] GarbagePool =
        [':', '/', '{', '}', '-', ',', '\n', '\r', '\0', '\t', 'Ø', 'ß', '9', 'C', 'D', 'R', '�'];

    [Fact]
    public void TryParse_never_throws_on_any_deterministic_mutation()
    {
        var cases = 0;
        foreach (var mutation in AllMutations())
        {
            cases++;
            var exception = Record.Exception(() => Mt940Parser.TryParse(mutation, out _));
            if (exception is not null)
            {
                Assert.Fail(
                    $"TryParse threw {exception.GetType().Name} on mutation #{cases}: " +
                    $"{exception.Message}\nInput was:\n{mutation}");
            }
        }

        Assert.True(
            cases >= MinimumTotalCases,
            $"Expected at least {MinimumTotalCases} fuzz cases, generated {cases}.");
    }

    [Fact]
    public void TryParse_stays_deterministic_across_runs()
    {
        foreach (var mutation in AllMutations().Take(50))
        {
            var first = Mt940Parser.TryParse(mutation, out var firstFile);
            var second = Mt940Parser.TryParse(mutation, out var secondFile);

            Assert.Equal(first, second);
            Assert.Equal(firstFile?.Statements.Count, secondFile?.Statements.Count);
        }
    }

    private static IEnumerable<string> AllMutations()
    {
        var random = new Random(RandomSeed);
        foreach (var fixture in TestFixtures.All)
        {
            var text = TestFixtures.ReadText(fixture);

            foreach (var truncation in TruncationsAtEveryTagBoundary(text))
            {
                yield return truncation;
            }

            foreach (var shuffled in ShuffledLines(text, random))
            {
                yield return shuffled;
            }

            foreach (var garbled in GarbageInsertions(text, random))
            {
                yield return garbled;
            }
        }
    }

    private static IEnumerable<string> TruncationsAtEveryTagBoundary(string text)
    {
        for (var index = text.IndexOf(':', 0); index >= 0; index = text.IndexOf(':', index + 1))
        {
            yield return text[..index];
            yield return text[..Math.Min(text.Length, index + 2)];
        }
    }

    private static IEnumerable<string> ShuffledLines(string text, Random random)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < ShufflesPerFixture; i++)
        {
            var shuffled = (string[])lines.Clone();
            random.Shuffle(shuffled);
            yield return string.Join('\n', shuffled);
        }
    }

    private static IEnumerable<string> GarbageInsertions(string text, Random random)
    {
        for (var i = 0; i < GarbageMutationsPerFixture; i++)
        {
            var builder = new StringBuilder(text);
            var edits = random.Next(1, 8);
            for (var edit = 0; edit < edits; edit++)
            {
                var position = random.Next(builder.Length + 1);
                builder.Insert(position, GarbagePool[random.Next(GarbagePool.Length)]);
            }

            yield return builder.ToString();
        }
    }
}
