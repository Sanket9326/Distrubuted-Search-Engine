using System.Text;
using Common.FileValidation;

namespace Services.TextExtraction;

public sealed class PlainTextExtractor : ITextExtractor
{
    public bool CanHandle(SupportedFileType fileType) =>
        fileType is SupportedFileType.Txt or SupportedFileType.Markdown;

    public async Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        content.Position = 0;
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
