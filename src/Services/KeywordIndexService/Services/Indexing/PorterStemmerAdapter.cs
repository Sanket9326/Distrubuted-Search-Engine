using Porter2StemmerStandard;

namespace Services.Indexing;

public sealed class PorterStemmerAdapter : IStemmer
{
    private readonly EnglishPorter2Stemmer _stemmer = new();

    public string Stem(string lowercasedToken) => _stemmer.Stem(lowercasedToken).Value;
}
