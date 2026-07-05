using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FileHandlerController : ControllerBase
{
    private readonly IFileHandlerService _fileHandlerService;

    public FileHandlerController(IFileHandlerService fileHandlerService)
    {
        _fileHandlerService = fileHandlerService;
    }

    /// <summary>
    /// Handles the file upload request.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        var result = await _fileHandlerService.HandleFileUploadAsync(file);

        if (result.IsSuccess)
        {
            return Ok(result.Message);
        }
        else
        {
            return StatusCode(500, result.Message);
        }
    }
}
