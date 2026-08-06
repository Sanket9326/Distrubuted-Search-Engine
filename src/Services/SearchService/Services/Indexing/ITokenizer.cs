namespace Services.Indexing;

/// <summary>A single raw token and its 0-based position in the original tokenization order.</summary>
public sealed record Token(string Value, int Position);

public interface ITokenizer
{
    IReadOnlyList<Token> Tokenize(string text);
}
