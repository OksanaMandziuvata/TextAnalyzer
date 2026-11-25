using Microsoft.AspNetCore.Http;

namespace TextAnalyzer.Api.DTOs;

/// <summary>Multipart form data for creating an analysis task.</summary>
public sealed record CreateTaskRequest
{
    /// <summary>The UTF-8 text file to analyze.</summary>
    public IFormFile? File { get; init; }

    /// <summary>The analysis mode: Basic or Frequency.</summary>
    public string? AnalysisType { get; init; }
}
