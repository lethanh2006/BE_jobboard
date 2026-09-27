using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Persistence;

public interface IJobApplicationRepository : IRepository<JobApplication>
{
    bool HasApplied(int candidateId, int jobId);
}
