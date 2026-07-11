public sealed class FileProcessingOptions
{
    public const string SectionName = "FileProcessing";

    public long MaxFileSizeBytes { get; init; } = 20 * 1024 * 1024;
}
