namespace TextAnalyzer.Api.DTOs;

/// <summary>Analysis statistics saved for a task.</summary>
public sealed record AnalysisResultResponse(
    Guid TaskId,
    string AnalysisType,
    int CharacterCount,
    int WordCount,
    int SentenceCount,
    int UniqueWordCount,
    string LongestWord,
    IReadOnlyDictionary<string, int> WordFrequencies,
    IReadOnlyList<WordFrequency> MostFrequentWords);
