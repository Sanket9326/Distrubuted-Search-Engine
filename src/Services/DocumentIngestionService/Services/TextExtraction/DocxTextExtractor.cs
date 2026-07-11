using Common.FileValidation;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Exceptions;

namespace Services.TextExtraction;

public sealed class DocxTextExtractor : ITextExtractor
{
    public bool CanHandle(SupportedFileType fileType) => fileType == SupportedFileType.Docx;

    public Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        content.Position = 0;

        try
        {
            using var wordDocument = WordprocessingDocument.Open(content, isEditable: false);
            var body = wordDocument.MainDocumentPart?.Document?.Body;
            var paragraphs = body?.Elements<Paragraph>() ?? Enumerable.Empty<Paragraph>();

            var text = string.Join(Environment.NewLine + Environment.NewLine, paragraphs.Select(p => p.InnerText));
            return Task.FromResult(text);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new TextExtractionException("Failed to extract text from DOCX content.", ex);
        }
    }
}
