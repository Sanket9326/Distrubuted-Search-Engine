using Common.FileValidation;
using Exceptions;

namespace Services.TextExtraction;

public interface ITextExtractorResolver
{
    ITextExtractor Resolve(SupportedFileType fileType);
}

public sealed class TextExtractorResolver : ITextExtractorResolver
{
    private readonly IEnumerable<ITextExtractor> _extractors;

    public TextExtractorResolver(IEnumerable<ITextExtractor> extractors)
    {
        _extractors = extractors;
    }

    public ITextExtractor Resolve(SupportedFileType fileType)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanHandle(fileType));
        if (extractor is null)
        {
            throw new UnsupportedFileTypeException($"No text extractor is registered for file type '{fileType}'.");
        }

        return extractor;
    }
}
