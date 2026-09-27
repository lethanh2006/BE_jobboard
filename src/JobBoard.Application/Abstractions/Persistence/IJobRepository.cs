using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Persistence;

public interface IJobRepository : IRepository<Job>
{
    Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default);
}
