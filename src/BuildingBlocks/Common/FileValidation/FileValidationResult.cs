namespace Common.FileValidation;

public sealed class FileValidationResult
{
    public bool IsValid { get; init; }

    public SupportedFileType? FileType { get; init; }

    public string? ErrorMessage { get; init; }

    public static FileValidationResult Success(SupportedFileType fileType) =>
        new() { IsValid = true, FileType = fileType };

    public static FileValidationResult Failure(string errorMessage) =>
        new() { IsValid = false, ErrorMessage = errorMessage };
}
