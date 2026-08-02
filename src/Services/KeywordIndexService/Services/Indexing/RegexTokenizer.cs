using System.Text.RegularExpressions;

namespace Services.Indexing;

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
