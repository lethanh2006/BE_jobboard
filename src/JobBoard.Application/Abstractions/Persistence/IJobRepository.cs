using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Persistence;

public interface IJobRepository : IRepository<Job>
{
    Task<int> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default);
}
