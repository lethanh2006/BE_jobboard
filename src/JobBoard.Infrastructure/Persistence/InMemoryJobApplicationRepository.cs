using System.Collections.Concurrent;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence;

public sealed class InMemoryJobApplicationRepository : IJobApplicationRepository
{
    private readonly ConcurrentDictionary<int, JobApplication> _applications = new();

    public Task<JobApplication?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _applications.TryGetValue(id, out var application);

        return Task.FromResult(application);
    }

    public Task AddAsync(JobApplication entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();

        if (!_applications.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"Đơn ứng tuyển có mã {entity.Id} đã tồn tại.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(JobApplication entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();
        _applications[entity.Id] = entity;

        return Task.CompletedTask;
    }

    public Task<bool> HasAppliedAsync(
        int candidateId,
        int jobId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hasApplied = _applications.Values.Any(application =>
            application.CandidateId == candidateId && application.JobId == jobId);

        return Task.FromResult(hasApplied);
    }
}
