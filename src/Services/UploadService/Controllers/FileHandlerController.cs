using Common.FileValidation;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class FileHandlerController : ControllerBase
{
    private readonly IFileHandlerService _fileHandlerService;
    private readonly IFileSignatureValidator _fileSignatureValidator;

    public FileHandlerController(IFileHandlerService fileHandlerService, IFileSignatureValidator fileSignatureValidator)
    {
        _fileHandlerService = fileHandlerService;
        _fileSignatureValidator = fileSignatureValidator;
    }

    /// <summary>
    /// Handles the file upload request.
    /// </summary>
    /// <param name="file">The file to upload.</param>
    /// <param name="departments">Comma-separated department names authorized to access this document (e.g. "Finance,Engineering").</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string? departments = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        await using var validationStream = file.OpenReadStream();
        var validationResult = await _fileSignatureValidator.ValidateAsync(file.FileName, validationStream, HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ErrorMessage);
        }

        var result = await _fileHandlerService.HandleFileUploadAsync(file, departments);

        if (result.IsSuccess)
        {
            return Ok(result.Event);
        }
        else
        {
            return StatusCode(500, "Failed to upload file.");
        }
    }
}
