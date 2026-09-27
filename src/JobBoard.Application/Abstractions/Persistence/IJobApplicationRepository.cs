using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Persistence;

public interface IJobApplicationRepository : IRepository<JobApplication>
{
    Task<bool> HasAppliedAsync(
        int candidateId,
        int jobId,
        CancellationToken cancellationToken = default);
}
