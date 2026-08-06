namespace Services.Indexing;

public interface IStopWordFilter
{
    bool IsStopWord(string lowercasedToken);
}
