using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TextAnalyzer.Api.Data;

namespace TextAnalyzer.Api.Tests;

[CollectionDefinition("Task API integration", DisableParallelization = true)]
public sealed class TaskApiTestCollection : ICollectionFixture<TaskApiTestFixture>
{
}

public sealed class TaskApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString =
        Environment.GetEnvironmentVariable("TEXTANALYZER_TEST_CONNECTION_STRING")
        ?? throw new InvalidOperationException(
            "Set TEXTANALYZER_TEST_CONNECTION_STRING to a dedicated test database.");
    private readonly string? _originalConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

    public TaskApiTestFixture()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        Dispose();
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            _originalConnectionString);
        return Task.CompletedTask;
    }

    public async Task ClearDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Results.ExecuteDeleteAsync();
        await dbContext.Tasks.ExecuteDeleteAsync();
    }
}
