namespace TextAnalyzer.Api.DTOs;

/// <summary>Word and number of occurrences in analyzed text.</summary>
public sealed record WordFrequency(string Word, int Count);
