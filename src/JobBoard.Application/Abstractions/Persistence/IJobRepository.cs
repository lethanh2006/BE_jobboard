using JobBoard.Domain.Entities;

namespace JobBoard.Application.Abstractions.Persistence;

public interface IJobRepository : IRepository<Job>
{
    IReadOnlyCollection<Job> GetAll();
}
