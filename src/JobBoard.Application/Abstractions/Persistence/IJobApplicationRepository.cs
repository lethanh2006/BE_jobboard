using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Persistence;

public interface IJobApplicationRepository : IRepository<JobApplication>
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<JobApplication>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> HasAppliedAsync(
        int candidateId,
        int jobId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<JobApplication>> GetByCandidateIdAsync(
        int candidateId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<JobApplication>> GetByJobIdAsync(
        int jobId,
        CancellationToken cancellationToken = default);
}
