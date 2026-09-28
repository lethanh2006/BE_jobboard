using System.Collections.Concurrent;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence;

public sealed class InMemoryJobApplicationRepository : IJobApplicationRepository
{
    private readonly ConcurrentDictionary<int, JobApplication> _applications = new();
    private readonly object _identityLock = new();
    private int _lastId;

    public InMemoryJobApplicationRepository(bool seedDemoData = false)
    {
        if (!seedDemoData)
        {
            return;
        }

        var seeds = new[]
        {
            JobApplication.Restore(
                1,
                2,
                20,
                new DateTime(2026, 9, 25, 8, 0, 0, DateTimeKind.Utc),
                ApplicationStatus.Reviewing),
            JobApplication.Restore(
                2,
                5,
                20,
                new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc),
                ApplicationStatus.Submitted),
            JobApplication.Restore(
                3,
                6,
                20,
                new DateTime(2026, 9, 12, 8, 0, 0, DateTimeKind.Utc),
                ApplicationStatus.Rejected)
        };

        foreach (var application in seeds)
        {
            _applications[application.Id] = application;
        }

        _lastId = seeds.Max(application => application.Id);
    }

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

        lock (_identityLock)
        {
            _lastId = Math.Max(_lastId, entity.Id);
        }

        return Task.CompletedTask;
    }

    public Task<int> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_identityLock)
        {
            _lastId++;
            return Task.FromResult(_lastId);
        }
    }

    public Task<IReadOnlyCollection<JobApplication>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<JobApplication>>(_applications.Values.ToArray());
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

    public Task<IReadOnlyCollection<JobApplication>> GetByCandidateIdAsync(
        int candidateId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = _applications.Values
            .Where(application => application.CandidateId == candidateId)
            .OrderByDescending(application => application.AppliedAt)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<JobApplication>>(result);
    }

    public Task<IReadOnlyCollection<JobApplication>> GetByJobIdAsync(
        int jobId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = _applications.Values
            .Where(application => application.JobId == jobId)
            .OrderByDescending(application => application.AppliedAt)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<JobApplication>>(result);
    }
}
