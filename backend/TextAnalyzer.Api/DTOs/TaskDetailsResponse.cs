namespace TextAnalyzer.Api.DTOs;

/// <summary>Task information including its current processing status.</summary>
public sealed record TaskDetailsResponse(
    Guid Id,
    string FileName,
    string AnalysisType,
    string Status,
    int Progress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
