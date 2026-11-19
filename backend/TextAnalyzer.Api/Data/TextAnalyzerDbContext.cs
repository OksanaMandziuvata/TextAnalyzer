using Microsoft.EntityFrameworkCore;

namespace TextAnalyzer.Api.Data;

public class TextAnalyzerDbContext : DbContext
{
    public TextAnalyzerDbContext(DbContextOptions<TextAnalyzerDbContext> options)
        : base(options)
    {
    }
}
