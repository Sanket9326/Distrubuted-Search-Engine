using Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchProcessingService _searchProcessingService;
    private readonly SearchOptions _options;

    public SearchController(ISearchProcessingService searchProcessingService, IOptions<SearchOptions> options)
    {
        _searchProcessingService = searchProcessingService;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query is required.");
        }

        if (request.Query.Length > _options.MaxQueryLength)
        {
            return BadRequest($"Query exceeds the maximum length of {_options.MaxQueryLength} characters.");
        }

        var response = await _searchProcessingService.SearchAsync(request, HttpContext.RequestAborted);

        return Ok(response);
    }
}
