using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TextAnalyzer.Api.DTOs;

namespace TextAnalyzer.Api.Tests;

[Collection("Task API integration")]
public class TasksControllerTests
{
    private readonly TaskApiTestFixture _fixture;

    public TasksControllerTests(TaskApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAndReadTask_ReturnsSavedTaskAndResult()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();

        using var firstResponse = await client.PostAsync(
            "/api/tasks",
            CreateForm("first.txt", "Hello world. Hello programming!", "Frequency"));
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.NotNull(firstResponse.Headers.Location);

        var created = await firstResponse.Content.ReadFromJsonAsync<CreateTaskResponse>();
        Assert.NotNull(created);
        Assert.Equal(4, created.Result.WordCount);
        Assert.Equal(3, created.Result.UniqueWordCount);
        Assert.Equal(2, created.Result.WordFrequencies["hello"]);

        using var secondResponse = await client.PostAsync(
            "/api/tasks",
            CreateForm("second.txt", "Привіт світе.", "Basic"));
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var secondTask = await secondResponse.Content.ReadFromJsonAsync<CreateTaskResponse>();
        Assert.NotNull(secondTask);

        var history = await client.GetFromJsonAsync<List<TaskSummaryResponse>>("/api/tasks");
        Assert.NotNull(history);
        Assert.Equal(2, history.Count);
        Assert.Equal(secondTask.Id, history[0].Id);
        Assert.Equal(created.Id, history[1].Id);

        var details = await client.GetFromJsonAsync<TaskDetailsResponse>(
            $"/api/tasks/{created.Id}");
        Assert.NotNull(details);
        Assert.Equal("Completed", details.Status);
        Assert.Equal(100, details.Progress);

        var savedResult = await client.GetFromJsonAsync<AnalysisResultResponse>(
            $"/api/tasks/{created.Id}/result");
        Assert.NotNull(savedResult);
        Assert.Equal(created.Id, savedResult.TaskId);
        Assert.Equal(2, savedResult.WordFrequencies["hello"]);
    }

    [Fact]
    public async Task Create_WithLongWordWithinFileLimit_SavesResult()
    {
        await _fixture.ClearDatabaseAsync();
        using var client = _fixture.CreateClient();
        var text = new string('a', 1000);

        using var response = await client.PostAsync(
            "/api/tasks",
            CreateForm("long-word.txt", text, "Basic"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreateTaskResponse>();
        Assert.NotNull(created);
        Assert.Equal(1000, created.Result.LongestWord.Length);
    }

    [Fact]
    public async Task Create_WithInvalidExtension_ReturnsBadRequest()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync(
            "/api/tasks",
            CreateForm("document.pdf", "text", "Basic"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyFile_ReturnsBadRequest()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync(
            "/api/tasks",
            CreateForm("empty.txt", string.Empty, "Basic"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithFileOverTwoMegabytes_ReturnsBadRequest()
    {
        using var client = _fixture.CreateClient();
        var content = new ByteArrayContent(new byte[2 * 1024 * 1024 + 1]);
        using var form = new MultipartFormDataContent
        {
            { content, "file", "large.txt" },
            { new StringContent("Basic"), "analysisType" }
        };

        using var response = await client.PostAsync("/api/tasks", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidUtf8_ReturnsBadRequest()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync(
            "/api/tasks",
            CreateForm("invalid.txt", [0xC3, 0x28], "Basic"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnsupportedAnalysisType_ReturnsBadRequest()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.PostAsync(
            "/api/tasks",
            CreateForm("text.txt", "some text", "Detailed"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTaskAndResult_WhenTaskDoesNotExist_ReturnsNotFound()
    {
        using var client = _fixture.CreateClient();
        var id = Guid.NewGuid();

        using var taskResponse = await client.GetAsync($"/api/tasks/{id}");
        using var resultResponse = await client.GetAsync($"/api/tasks/{id}/result");

        Assert.Equal(HttpStatusCode.NotFound, taskResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, resultResponse.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocument_ContainsTaskEndpoints()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/tasks", out _));
        Assert.True(paths.TryGetProperty("/api/tasks/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/tasks/{id}/result", out _));
    }

    private static MultipartFormDataContent CreateForm(
        string fileName,
        string text,
        string analysisType)
    {
        return CreateForm(fileName, Encoding.UTF8.GetBytes(text), analysisType);
    }

    private static MultipartFormDataContent CreateForm(
        string fileName,
        byte[] fileBytes,
        string analysisType)
    {
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        return new MultipartFormDataContent
        {
            { fileContent, "file", fileName },
            { new StringContent(analysisType), "analysisType" }
        };
    }
}
