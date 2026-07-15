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
    private readonly IAnswerService _answerService;
    private readonly SearchOptions _options;

    public SearchController(
        ISearchProcessingService searchProcessingService,
        IAnswerService answerService,
        IOptions<SearchOptions> options)
    {
        _searchProcessingService = searchProcessingService;
        _answerService = answerService;
        _options = options.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var validationError = ValidateQuery(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var response = await _searchProcessingService.SearchAsync(request, HttpContext.RequestAborted);

        return Ok(response);
    }

    [HttpPost("answer")]
    public async Task<IActionResult> Answer([FromBody] SearchRequest request)
    {
        var validationError = ValidateQuery(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var response = await _answerService.AnswerAsync(request, HttpContext.RequestAborted);

        return Ok(response);
    }

    private IActionResult? ValidateQuery(SearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query is required.");
        }

        if (request.Query.Length > _options.MaxQueryLength)
        {
            return BadRequest($"Query exceeds the maximum length of {_options.MaxQueryLength} characters.");
        }

        return null;
    }
}
