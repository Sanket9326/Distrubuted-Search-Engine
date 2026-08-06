using System.Text.RegularExpressions;

namespace Services.Indexing;

// Mirrors KeywordIndexService's RegexTokenizer exactly - query-time tokenization must match
// index-time tokenization term-for-term, or BM25 term lookups won't find postings. Kept as a
// per-service duplicate rather than shared code, per this repo's "no shared code between
// services" convention.
public sealed partial class RegexTokenizer : ITokenizer
{
    public IReadOnlyList<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var position = 0;

        foreach (Match match in WordPattern().Matches(text))
        {
            tokens.Add(new Token(match.Value, position));
            position++;
        }

        return tokens;
    }

    [GeneratedRegex(@"[\w']+", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
