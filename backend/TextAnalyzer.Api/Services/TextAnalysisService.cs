using System.Globalization;
using System.Text;
using TextAnalyzer.Api.DTOs;

namespace TextAnalyzer.Api.Services;

public class TextAnalysisService
{
    private const int MostFrequentWordLimit = 10;

    public TextAnalysisResult Analyze(
        string text,
        TextAnalysisType analysisType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!Enum.IsDefined(analysisType))
        {
            throw new ArgumentOutOfRangeException(nameof(analysisType));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var runes = new List<Rune>();
        foreach (var rune in text.EnumerateRunes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            runes.Add(rune);
        }

        var frequencies = new Dictionary<string, int>(StringComparer.Ordinal);
        var wordBuilder = new StringBuilder();
        var characterCount = runes.Count;
        var sentenceCount = 0;
        var hasSentenceContent = false;
        var longestWord = string.Empty;
        var longestWordLength = 0;

        for (var index = 0; index < runes.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rune = runes[index];

            if (IsWordLetter(rune))
            {
                wordBuilder.Append(rune.ToString());
                if (Rune.IsLetterOrDigit(rune))
                {
                    hasSentenceContent = true;
                }

                continue;
            }

            if (IsApostrophe(rune)
                && wordBuilder.Length > 0
                && index + 1 < runes.Count
                && IsWordLetter(runes[index + 1]))
            {
                wordBuilder.Append('\'');
                continue;
            }

            if (IsSentenceTerminator(rune))
            {
                if (hasSentenceContent)
                {
                    sentenceCount++;
                    hasSentenceContent = false;
                }

                while (index + 1 < runes.Count && IsSentenceTerminator(runes[index + 1]))
                {
                    index++;
                }
            }

            AddWord(wordBuilder, frequencies, ref longestWord, ref longestWordLength);
        }

        AddWord(wordBuilder, frequencies, ref longestWord, ref longestWordLength);

        var result = new TextAnalysisResult
        {
            AnalysisType = analysisType,
            CharacterCount = characterCount,
            WordCount = frequencies.Values.Sum(),
            SentenceCount = sentenceCount,
            UniqueWordCount = frequencies.Count,
            LongestWord = longestWord
        };

        if (analysisType == TextAnalysisType.Frequency)
        {
            result.WordFrequencies = frequencies;
            result.MostFrequentWords = frequencies
                .OrderByDescending(frequency => frequency.Value)
                .ThenBy(frequency => frequency.Key, StringComparer.Ordinal)
                .Take(MostFrequentWordLimit)
                .Select(frequency => new WordFrequency(frequency.Key, frequency.Value))
                .ToList();
        }

        return result;
    }

    private static void AddWord(
        StringBuilder wordBuilder,
        Dictionary<string, int> frequencies,
        ref string longestWord,
        ref int longestWordLength)
    {
        if (wordBuilder.Length == 0)
        {
            return;
        }

        var word = wordBuilder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();

        frequencies.TryGetValue(word, out var count);
        frequencies[word] = count + 1;

        var wordLength = 0;
        foreach (var rune in word.EnumerateRunes())
        {
            wordLength++;
        }

        if (wordLength > longestWordLength)
        {
            longestWord = word;
            longestWordLength = wordLength;
        }

        wordBuilder.Clear();
    }

    private static bool IsWordLetter(Rune rune)
    {
        var category = Rune.GetUnicodeCategory(rune);
        return Rune.IsLetter(rune)
            || category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark;
    }

    private static bool IsApostrophe(Rune rune)
    {
        return rune.Value is '\'' or '\u2019' or '\u02BC';
    }

    private static bool IsSentenceTerminator(Rune rune)
    {
        return rune.Value is '.' or '!' or '?';
    }
}
