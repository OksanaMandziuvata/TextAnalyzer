namespace TextAnalyzer.Api.Models;

public class AnalysisResult
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public int CharacterCount { get; set; }

    public int WordCount { get; set; }

    public int SentenceCount { get; set; }

    public int UniqueWordCount { get; set; }

    public string LongestWord { get; set; } = string.Empty;

    public string WordFrequenciesJson { get; set; } = "{}";

    public AnalysisTask Task { get; set; } = null!;
}
