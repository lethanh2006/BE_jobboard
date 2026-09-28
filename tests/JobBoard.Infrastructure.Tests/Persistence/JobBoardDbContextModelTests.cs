using JobBoard.Infrastructure.Persistence.Database;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Tests.Persistence;

public sealed class JobBoardDbContextModelTests
{
    [Fact]
    public void Model_DefinesApplicationUniquenessAndDatabaseSequences()
    {
        var options = new DbContextOptionsBuilder<JobBoardDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=test;Password=test")
            .Options;
        using var dbContext = new JobBoardDbContext(options);

        var application = dbContext.Model.FindEntityType(typeof(JobApplicationRecord));
        var uniqueIndex = application?.GetIndexes().SingleOrDefault(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(JobApplicationRecord.CandidateId), nameof(JobApplicationRecord.JobId)]));

        Assert.NotNull(uniqueIndex);
        Assert.Equal(7, dbContext.Model.FindSequence("job_ids")?.StartValue);
        Assert.Equal(4, dbContext.Model.FindSequence("application_ids")?.StartValue);
    }
}
