using System.Text;
using Common.FileValidation;
using Exceptions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace Services.TextExtraction;

public sealed class PdfDocumentTextExtractor : ITextExtractor
{
    public bool CanHandle(SupportedFileType fileType) => fileType == SupportedFileType.Pdf;

    public Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        content.Position = 0;

        try
        {
            using var pdfReader = new PdfReader(content);
            using var pdfDocument = new PdfDocument(pdfReader);

            var sb = new StringBuilder();
            for (var pageNumber = 1; pageNumber <= pdfDocument.GetNumberOfPages(); pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = pdfDocument.GetPage(pageNumber);
                sb.Append(PdfTextExtractor.GetTextFromPage(page));
                sb.Append(Environment.NewLine).Append(Environment.NewLine);
            }

            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new TextExtractionException("Failed to extract text from PDF content.", ex);
        }
    }
}
