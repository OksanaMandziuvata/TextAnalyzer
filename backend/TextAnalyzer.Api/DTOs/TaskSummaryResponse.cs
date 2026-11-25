namespace TextAnalyzer.Api.DTOs;

/// <summary>Brief information about an analysis task.</summary>
public sealed record TaskSummaryResponse(
    Guid Id,
    string FileName,
    string AnalysisType,
    string Status,
    int Progress,
    DateTimeOffset CreatedAt);
