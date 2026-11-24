namespace TextAnalyzer.Api.Models;

public class AnalysisTask
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string AnalysisType { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int Progress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public AnalysisResult? Result { get; set; }
}
