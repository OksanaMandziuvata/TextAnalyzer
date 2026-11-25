using TextAnalyzer.Api.DTOs;

namespace TextAnalyzer.Api.Services;

public interface ITaskService
{
    Task<CreateTaskResponse> CreateAsync(
        string fileName,
        string text,
        TextAnalysisType analysisType,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskSummaryResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<TaskDetailsResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<AnalysisResultResponse?> GetResultAsync(
        Guid id,
        CancellationToken cancellationToken);
}
