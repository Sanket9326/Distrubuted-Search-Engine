using System.Text;

namespace Common.FileValidation;

public sealed class FileSignatureValidator : IFileSignatureValidator
{
    private const int SignatureSampleSize = 4096;

    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];

    private static readonly IReadOnlyDictionary<string, SupportedFileType> ExtensionMap = new Dictionary<string, SupportedFileType>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = SupportedFileType.Pdf,
        [".docx"] = SupportedFileType.Docx,
        [".txt"] = SupportedFileType.Txt,
        [".md"] = SupportedFileType.Markdown
    };

    public async Task<FileValidationResult> ValidateAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (!content.CanSeek)
        {
            throw new ArgumentException("Stream must support seeking for file signature validation.", nameof(content));
        }

        var extension = Path.GetExtension(fileName);
        if (!ExtensionMap.TryGetValue(extension, out var fileType))
        {
            return FileValidationResult.Failure(
                $"File extension '{extension}' is not supported. Allowed types: PDF, DOCX, TXT, MD.");
        }

        content.Position = 0;
        var buffer = new byte[SignatureSampleSize];
        var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        content.Position = 0;

        var sample = buffer.AsSpan(0, bytesRead);

        var contentMatches = fileType switch
        {
            SupportedFileType.Pdf => sample.StartsWith(PdfSignature),
            SupportedFileType.Docx => sample.StartsWith(ZipSignature),
            SupportedFileType.Txt or SupportedFileType.Markdown => LooksLikeText(sample),
            _ => false
        };

        return contentMatches
            ? FileValidationResult.Success(fileType)
            : FileValidationResult.Failure(
                $"File content does not match the expected signature for a '{extension}' file.");
    }

    private static bool LooksLikeText(ReadOnlySpan<byte> sample)
    {
        if (sample.IsEmpty)
        {
            return false;
        }

        try
        {
            new UTF8Encoding(false, true).GetString(sample);
        }
        catch (DecoderFallbackException)
        {
            return false;
        }

        var controlCharCount = 0;
        foreach (var b in sample)
        {
            var isCommonWhitespace = b is 0x09 or 0x0A or 0x0D;
            if (b < 0x20 && !isCommonWhitespace)
            {
                controlCharCount++;
            }
        }

        return controlCharCount == 0;
    }
}
