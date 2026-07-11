using System.Text;
using System.Text.RegularExpressions;
using Common.Utilities;
using Entities;

namespace Services.Chunking;

/// <summary>
/// Splits text into overlapping, paragraph-aware chunks (sliding window with overlap),
/// falling back to sentence- then whitespace-splitting for paragraphs that alone exceed the max chunk size.
/// </summary>
public sealed class ParagraphAwareChunkingService : IChunkingService
{
    private static readonly Regex ParagraphSplitRegex = new(@"\r?\n\s*\r?\n", RegexOptions.Compiled);
    private static readonly Regex SentenceSplitRegex = new(@"(?<=[.!?])\s+", RegexOptions.Compiled);

    private readonly IGuidGenerator _guidGenerator;

    public ParagraphAwareChunkingService(IGuidGenerator guidGenerator)
    {
        _guidGenerator = guidGenerator;
    }

    public IReadOnlyList<DocumentChunk> Chunk(string documentId, string text, ChunkingOptions options)
    {
        var chunks = new List<DocumentChunk>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return chunks;
        }

        var createdAtUtc = DateTime.UtcNow;
        var chunkIndex = 0;
        var buffer = new StringBuilder();

        foreach (var segment in SplitIntoSegments(text, options.MaxChunkSize))
        {
            if (buffer.Length > 0 && buffer.Length + segment.Length + 2 > options.MaxChunkSize)
            {
                chunks.Add(CreateChunk(documentId, chunkIndex++, buffer.ToString(), createdAtUtc));
                buffer = CarryOverlap(buffer.ToString(), options.OverlapSize);
            }

            if (buffer.Length > 0)
            {
                buffer.Append(Environment.NewLine).Append(Environment.NewLine);
            }

            buffer.Append(segment);
        }

        if (buffer.Length > 0)
        {
            chunks.Add(CreateChunk(documentId, chunkIndex, buffer.ToString(), createdAtUtc));
        }

        return chunks;
    }

    private static IEnumerable<string> SplitIntoSegments(string text, int maxChunkSize)
    {
        var paragraphs = ParagraphSplitRegex.Split(text)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0);

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length <= maxChunkSize)
            {
                yield return paragraph;
                continue;
            }

            foreach (var piece in SplitOversizedParagraph(paragraph, maxChunkSize))
            {
                yield return piece;
            }
        }
    }

    private static IEnumerable<string> SplitOversizedParagraph(string paragraph, int maxChunkSize)
    {
        var sentences = SentenceSplitRegex.Split(paragraph);
        var buffer = new StringBuilder();

        foreach (var sentence in sentences)
        {
            if (sentence.Length > maxChunkSize)
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }

                foreach (var piece in SplitByWhitespace(sentence, maxChunkSize))
                {
                    yield return piece;
                }

                continue;
            }

            if (buffer.Length > 0 && buffer.Length + sentence.Length + 1 > maxChunkSize)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }

            if (buffer.Length > 0)
            {
                buffer.Append(' ');
            }

            buffer.Append(sentence);
        }

        if (buffer.Length > 0)
        {
            yield return buffer.ToString();
        }
    }

    private static IEnumerable<string> SplitByWhitespace(string text, int maxChunkSize)
    {
        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(maxChunkSize, text.Length - start);
            var end = start + length;

            if (end < text.Length)
            {
                var lastSpace = text.LastIndexOf(' ', end - 1, length);
                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            yield return text[start..end].Trim();
            start = end;
        }
    }

    private static StringBuilder CarryOverlap(string previousChunk, int overlapSize)
    {
        if (overlapSize <= 0 || previousChunk.Length <= overlapSize)
        {
            return new StringBuilder();
        }

        var start = previousChunk.Length - overlapSize;
        var spaceIndex = previousChunk.IndexOf(' ', start);
        if (spaceIndex >= 0 && spaceIndex < previousChunk.Length - 1)
        {
            start = spaceIndex + 1;
        }

        return new StringBuilder(previousChunk[start..]);
    }

    private DocumentChunk CreateChunk(string documentId, int chunkIndex, string content, DateTime createdAtUtc) =>
        new()
        {
            Id = _guidGenerator.NewGuid(),
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Content = content,
            CharCount = content.Length,
            CreatedAtUtc = createdAtUtc
        };
}
