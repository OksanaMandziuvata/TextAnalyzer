using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TextAnalyzer.Api.Data;
using TextAnalyzer.Api.DTOs;
using TextAnalyzer.Api.Models;

namespace TextAnalyzer.Api.Services;

public sealed class TaskService : ITaskService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TextAnalysisService _textAnalysisService;

    public TaskService(
        ApplicationDbContext dbContext,
        TextAnalysisService textAnalysisService)
    {
        _dbContext = dbContext;
        _textAnalysisService = textAnalysisService;
    }

    public async Task<CreateTaskResponse> CreateAsync(
        string fileName,
        string text,
        TextAnalysisType analysisType,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTime.UtcNow;
        var analysis = _textAnalysisService.Analyze(text, analysisType, cancellationToken);
        var completedAt = DateTime.UtcNow;

        var task = new AnalysisTask
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            AnalysisType = analysisType.ToString(),
            Status = "Completed",
            Progress = 100,
            CreatedAt = startedAt,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };

        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            CharacterCount = analysis.CharacterCount,
            WordCount = analysis.WordCount,
            SentenceCount = analysis.SentenceCount,
            UniqueWordCount = analysis.UniqueWordCount,
            LongestWord = analysis.LongestWord,
            WordFrequenciesJson = JsonSerializer.Serialize(analysis.WordFrequencies),
            Task = task
        };

        task.Result = result;
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateTaskResponse(
            task.Id,
            task.Status,
            ToResponse(task, result, analysis.WordFrequencies, analysis.MostFrequentWords));
    }

    public async Task<IReadOnlyList<TaskSummaryResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.Tasks
            .AsNoTracking()
            .OrderByDescending(task => task.CreatedAt)
            .Select(task => new
            {
                task.Id,
                task.FileName,
                task.AnalysisType,
                task.Status,
                task.Progress,
                task.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return tasks
            .Select(task => new TaskSummaryResponse(
                task.Id,
                task.FileName,
                task.AnalysisType,
                task.Status,
                task.Progress,
                AsUtcOffset(task.CreatedAt)))
            .ToArray();
    }

    public async Task<TaskDetailsResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        return task is null
            ? null
            : new TaskDetailsResponse(
                task.Id,
                task.FileName,
                task.AnalysisType,
                task.Status,
                task.Progress,
                AsUtcOffset(task.CreatedAt),
                task.StartedAt is null ? null : AsUtcOffset(task.StartedAt.Value),
                task.CompletedAt is null ? null : AsUtcOffset(task.CompletedAt.Value),
                task.ErrorMessage);
    }

    public async Task<AnalysisResultResponse?> GetResultAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var task = await _dbContext.Tasks
            .AsNoTracking()
            .Include(task => task.Result)
            .SingleOrDefaultAsync(task => task.Id == id, cancellationToken);

        if (task?.Result is null)
        {
            return null;
        }

        var frequencies = JsonSerializer.Deserialize<Dictionary<string, int>>(
            task.Result.WordFrequenciesJson) ?? new Dictionary<string, int>();
        var mostFrequentWords = task.AnalysisType == TextAnalysisType.Frequency.ToString()
            ? GetMostFrequentWords(frequencies)
            : Array.Empty<WordFrequency>();

        return ToResponse(task, task.Result, frequencies, mostFrequentWords);
    }

    private static AnalysisResultResponse ToResponse(
        AnalysisTask task,
        AnalysisResult result,
        IReadOnlyDictionary<string, int> frequencies,
        IReadOnlyList<WordFrequency> mostFrequentWords)
    {
        return new AnalysisResultResponse(
            task.Id,
            task.AnalysisType,
            result.CharacterCount,
            result.WordCount,
            result.SentenceCount,
            result.UniqueWordCount,
            result.LongestWord,
            frequencies,
            mostFrequentWords);
    }

    private static IReadOnlyList<WordFrequency> GetMostFrequentWords(
        IReadOnlyDictionary<string, int> frequencies)
    {
        return frequencies
            .OrderByDescending(frequency => frequency.Value)
            .ThenBy(frequency => frequency.Key, StringComparer.Ordinal)
            .Take(10)
            .Select(frequency => new WordFrequency(frequency.Key, frequency.Value))
            .ToArray();
    }

    private static DateTimeOffset AsUtcOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
