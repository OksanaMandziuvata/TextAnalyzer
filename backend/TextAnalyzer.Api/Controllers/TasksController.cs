using System.Text;
using Microsoft.AspNetCore.Mvc;
using TextAnalyzer.Api.DTOs;
using TextAnalyzer.Api.Services;

namespace TextAnalyzer.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private const long MaximumFileSize = 2 * 1024 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>Analyzes a UTF-8 TXT file and saves the task and result.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateTaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateTaskResponse>> Create(
        [FromForm] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
        {
            return ValidationError("file", "A TXT file is required.");
        }

        if (!string.Equals(
                Path.GetExtension(request.File.FileName),
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            return ValidationError("file", "Only .txt files are allowed.");
        }

        var fileName = Path.GetFileName(request.File.FileName.Replace('\\', '/'));
        if (fileName.Length > 255)
        {
            return ValidationError("file", "The file name must not exceed 255 characters.");
        }

        if (request.File.Length > MaximumFileSize)
        {
            return ValidationError("file", "The file must not exceed 2 MB.");
        }

        if (request.File.Length == 0)
        {
            return ValidationError("file", "The file must not be empty.");
        }

        if (!TryParseAnalysisType(request.AnalysisType, out var analysisType))
        {
            return ValidationError("analysisType", "Choose Basic or Frequency.");
        }

        await using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);

        var bytes = memoryStream.ToArray();
        var offset = HasUtf8Bom(bytes) ? 3 : 0;

        string text;
        try
        {
            text = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException)
        {
            return ValidationError("file", "The file must contain valid UTF-8 text.");
        }

        if (text.Length == 0)
        {
            return ValidationError("file", "The file must contain text.");
        }

        var result = await _taskService.CreateAsync(
            fileName,
            text,
            analysisType,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    /// <summary>Returns task history ordered from newest to oldest.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TaskSummaryResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var tasks = await _taskService.GetAllAsync(cancellationToken);
        return Ok(tasks);
    }

    /// <summary>Returns task details and its current status.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        return task is null ? NotFound() : Ok(task);
    }

    /// <summary>Returns the saved analysis result for a task.</summary>
    [HttpGet("{id:guid}/result")]
    [ProducesResponseType(typeof(AnalysisResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnalysisResultResponse>> GetResult(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetResultAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private BadRequestObjectResult ValidationError(string field, string message)
    {
        return BadRequest(new ValidationProblemDetails(
            new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request is invalid.",
            Instance = HttpContext.Request.Path
        });
    }

    private static bool TryParseAnalysisType(
        string? value,
        out TextAnalysisType analysisType)
    {
        if (string.Equals(value, nameof(TextAnalysisType.Basic), StringComparison.OrdinalIgnoreCase))
        {
            analysisType = TextAnalysisType.Basic;
            return true;
        }

        if (string.Equals(value, nameof(TextAnalysisType.Frequency), StringComparison.OrdinalIgnoreCase))
        {
            analysisType = TextAnalysisType.Frequency;
            return true;
        }

        analysisType = default;
        return false;
    }

    private static bool HasUtf8Bom(ReadOnlySpan<byte> bytes)
    {
        return bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF;
    }
}
