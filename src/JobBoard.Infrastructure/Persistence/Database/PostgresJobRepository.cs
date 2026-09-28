using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class PostgresJobRepository(JobBoardDbContext dbContext, IClock clock) : IJobRepository
{
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Jobs
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

        return record?.ToDomain(_clock);
    }

    public async Task<IReadOnlyCollection<Job>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        (await _dbContext.Jobs
            .AsNoTracking()
            .OrderBy(job => job.Id)
            .ToListAsync(cancellationToken))
        .Select(job => job.ToDomain(_clock))
        .ToArray();

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Database
            .SqlQueryRaw<int>("SELECT nextval('job_ids')::integer AS \"Value\"")
            .SingleAsync(cancellationToken);

    public async Task AddAsync(Job entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.Jobs.Add(entity.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await _dbContext.Jobs
            .SingleOrDefaultAsync(job => job.Id == entity.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Tin tuyển dụng có mã {entity.Id} không tồn tại.");

        record.UpdateFrom(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
