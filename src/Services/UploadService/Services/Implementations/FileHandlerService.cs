public class FileHandlerService : IFileHandlerService
{
    public async Task<(bool IsSuccess, string Message)> HandleFileUploadAsync(IFormFile file)
    {
        try
        {
            var storagePath = Path.Combine(
                     Directory.GetCurrentDirectory(),
                     "Storage");

            Directory.CreateDirectory(storagePath);

            var filePath = Path.Combine(storagePath, file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, "File uploaded successfully.");
        }
        catch (Exception ex)
        {
            return (false, $"File upload failed: {ex.Message}");
        }
    }
}