using TextAnalyzer.Api.DTOs;
using TextAnalyzer.Api.Services;

namespace TextAnalyzer.Api.Tests;

public class TextAnalysisServiceTests
{
    private readonly TextAnalysisService _service = new();

    [Fact]
    public void Analyze_EmptyText_ReturnsZeroCounts()
    {
        var result = _service.Analyze(string.Empty, TextAnalysisType.Basic);

        Assert.Equal(0, result.CharacterCount);
        Assert.Equal(0, result.WordCount);
        Assert.Equal(0, result.SentenceCount);
        Assert.Equal(0, result.UniqueWordCount);
        Assert.Equal(string.Empty, result.LongestWord);
        Assert.Empty(result.WordFrequencies);
        Assert.Empty(result.MostFrequentWords);
    }

    [Fact]
    public void Analyze_ExampleText_ReturnsExpectedCountsAndFrequencies()
    {
        var result = _service.Analyze(
            "Hello world. Hello programming!",
            TextAnalysisType.Frequency);

        Assert.Equal(4, result.WordCount);
        Assert.Equal(3, result.UniqueWordCount);
        Assert.Equal(2, result.SentenceCount);
        Assert.Equal("programming", result.LongestWord);
        Assert.Equal(2, result.WordFrequencies["hello"]);
        Assert.Equal(1, result.WordFrequencies["world"]);
        Assert.Equal(1, result.WordFrequencies["programming"]);
        Assert.Equal("hello", result.MostFrequentWords[0].Word);
    }

    [Fact]
    public void Analyze_UkrainianText_CountsUnicodeWords()
    {
        var result = _service.Analyze(
            "Україна — це країна. Україна!",
            TextAnalysisType.Frequency);

        Assert.Equal(4, result.WordCount);
        Assert.Equal(3, result.UniqueWordCount);
        Assert.Equal("україна", result.LongestWord);
        Assert.Equal(2, result.WordFrequencies["україна"]);
    }

    [Fact]
    public void Analyze_Apostrophes_NormalizesEnglishAndUkrainianForms()
    {
        var result = _service.Analyze(
            "DON'T don’t п’ять п'ять",
            TextAnalysisType.Frequency);

        Assert.Equal(4, result.WordCount);
        Assert.Equal(2, result.UniqueWordCount);
        Assert.Equal(2, result.WordFrequencies["don't"]);
        Assert.Equal(2, result.WordFrequencies["п'ять"]);
    }

    [Fact]
    public void Analyze_SupplementaryUnicodeCharacter_CountsAsOneCharacter()
    {
        var result = _service.Analyze("A😀", TextAnalysisType.Basic);

        Assert.Equal(2, result.CharacterCount);
        Assert.Equal(1, result.WordCount);
    }

    [Fact]
    public void Analyze_Punctuation_CountsSentencesAndIgnoresPunctuationInWords()
    {
        var result = _service.Analyze(
            "Hello, world! How are you? Fine...",
            TextAnalysisType.Basic);

        Assert.Equal(6, result.WordCount);
        Assert.Equal(3, result.SentenceCount);
    }

    [Fact]
    public void Analyze_Frequency_ReturnsTenMostFrequentWordsInStableOrder()
    {
        var text = "z z a a b b c c d d e e f f g g h h i i j j k k";

        var result = _service.Analyze(text, TextAnalysisType.Frequency);

        Assert.Equal(12, result.UniqueWordCount);
        Assert.Equal(10, result.MostFrequentWords.Count);
        Assert.Equal(
            new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" },
            result.MostFrequentWords.Select(word => word.Word));
        Assert.All(result.MostFrequentWords, word => Assert.Equal(2, word.Count));
    }

    [Fact]
    public void Analyze_CanceledToken_ThrowsOperationCanceledException()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => _service.Analyze("Some text", TextAnalysisType.Basic, cancellationSource.Token));
    }
}
