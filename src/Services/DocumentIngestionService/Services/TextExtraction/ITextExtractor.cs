using Common.FileValidation;

namespace Services.TextExtraction;

public interface ITextExtractor
{
    bool CanHandle(SupportedFileType fileType);

    Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}
