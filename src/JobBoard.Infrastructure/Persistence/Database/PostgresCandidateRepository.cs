using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class PostgresCandidateRepository(JobBoardDbContext dbContext) : ICandidateRepository
{
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<Candidate?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Candidates
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task AddAsync(Candidate entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.Candidates.Add(entity.ToRecord());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Candidate entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await _dbContext.Candidates
            .SingleOrDefaultAsync(candidate => candidate.Id == entity.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Ứng viên có mã {entity.Id} không tồn tại.");

        record.UpdateFrom(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
