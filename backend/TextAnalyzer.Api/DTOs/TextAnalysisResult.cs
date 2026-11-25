namespace TextAnalyzer.Api.DTOs;

public class TextAnalysisResult
{
    public TextAnalysisType AnalysisType { get; set; }

    public int CharacterCount { get; set; }

    public int WordCount { get; set; }

    public int SentenceCount { get; set; }

    public int UniqueWordCount { get; set; }

    public string LongestWord { get; set; } = string.Empty;

    public Dictionary<string, int> WordFrequencies { get; set; } = new();

    public List<WordFrequency> MostFrequentWords { get; set; } = new();
}
