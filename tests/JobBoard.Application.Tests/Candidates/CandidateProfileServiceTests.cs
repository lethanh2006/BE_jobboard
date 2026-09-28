using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Candidates;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Tests.Candidates;

public sealed class CandidateProfileServiceTests
{
    [Fact]
    public async Task UpdateAsync_PersistsProfileAndMapsDto()
    {
        var candidate = new Candidate(20, "Nguyễn Minh An", "old@example.com");
        var repository = new StubCandidateRepository(candidate);
        var service = new CandidateProfileService(repository);

        var result = await service.UpdateAsync(
            candidate.Id,
            new UpdateCandidateProfileCommand(
                "new@example.com",
                "Backend Developer",
                1800,
                2800,
                ["C#", "PostgreSQL"]));

        Assert.Equal("new@example.com", result.Email);
        Assert.Equal(1800, result.DesiredSalaryMin);
        Assert.Equal(["C#", "PostgreSQL"], result.Skills);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCandidateDoesNotExist_Throws()
    {
        var service = new CandidateProfileService(new StubCandidateRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.GetByIdAsync(999));
    }

    private sealed class StubCandidateRepository(params Candidate[] candidates) : ICandidateRepository
    {
        private readonly Dictionary<int, Candidate> _candidates =
            candidates.ToDictionary(candidate => candidate.Id);

        public int UpdateCount { get; private set; }

        public Task<Candidate?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_candidates.GetValueOrDefault(id));

        public Task AddAsync(Candidate entity, CancellationToken cancellationToken = default)
        {
            _candidates.Add(entity.Id, entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Candidate entity, CancellationToken cancellationToken = default)
        {
            _candidates[entity.Id] = entity;
            UpdateCount++;
            return Task.CompletedTask;
        }
    }
}
