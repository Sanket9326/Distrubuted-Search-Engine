using Porter2StemmerStandard;

namespace Services.Indexing;

// Mirrors KeywordIndexService's PorterStemmerAdapter exactly - kept as a per-service duplicate
// rather than shared code, per this repo's "no shared code between services" convention.
public sealed class PorterStemmerAdapter : IStemmer
{
    private readonly EnglishPorter2Stemmer _stemmer = new();

    public string Stem(string lowercasedToken) => _stemmer.Stem(lowercasedToken).Value;
}
