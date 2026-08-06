namespace Services.Indexing;

public interface IStemmer
{
    string Stem(string lowercasedToken);
}
