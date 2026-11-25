namespace TextAnalyzer.Api.DTOs;

/// <summary>Newly created analysis task and its result.</summary>
public sealed record CreateTaskResponse(
    Guid Id,
    string Status,
    AnalysisResultResponse Result);
