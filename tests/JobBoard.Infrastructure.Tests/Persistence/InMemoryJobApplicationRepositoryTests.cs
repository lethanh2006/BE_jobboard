using JobBoard.Domain.Entities;
using JobBoard.Infrastructure.Persistence;

namespace JobBoard.Infrastructure.Tests.Persistence;

public sealed class InMemoryJobApplicationRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsApplicationAndSupportsDuplicateCheck()
    {
        var repository = new InMemoryJobApplicationRepository();
        var application = new JobApplication(
            id: 1,
            jobId: 10,
            candidateId: 20,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(application);

        Assert.Same(application, await repository.GetByIdAsync(application.Id));
        Assert.True(await repository.HasAppliedAsync(candidateId: 20, jobId: 10));
        Assert.False(await repository.HasAppliedAsync(candidateId: 21, jobId: 10));
    }

    [Fact]
    public async Task SeededRepository_ReturnsCandidateHistoryAndNextIdentity()
    {
        var repository = new InMemoryJobApplicationRepository(seedDemoData: true);

        var history = await repository.GetByCandidateIdAsync(20);
        var nextId = await repository.GetNextIdAsync();

        Assert.Equal(3, history.Count);
        Assert.Equal(4, nextId);
    }
}
