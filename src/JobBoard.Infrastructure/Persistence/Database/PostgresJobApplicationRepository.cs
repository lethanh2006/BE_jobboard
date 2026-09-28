using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class PostgresJobApplicationRepository(JobBoardDbContext dbContext)
    : IJobApplicationRepository
{
    private readonly JobBoardDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<JobApplication?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Applications
            .AsNoTracking()
            .SingleOrDefaultAsync(application => application.Id == id, cancellationToken);

        return record?.ToDomain();
    }

    public async Task<int> GetNextIdAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Database
            .SqlQueryRaw<int>("SELECT nextval('application_ids')::integer AS \"Value\"")
            .SingleAsync(cancellationToken);

    public async Task<IReadOnlyCollection<JobApplication>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        (await _dbContext.Applications
            .AsNoTracking()
            .OrderBy(application => application.Id)
            .ToListAsync(cancellationToken))
        .Select(application => application.ToDomain())
        .ToArray();

    public Task<bool> HasAppliedAsync(
        int candidateId,
        int jobId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Applications
            .AsNoTracking()
            .AnyAsync(
                application => application.CandidateId == candidateId && application.JobId == jobId,
                cancellationToken);

    public async Task<IReadOnlyCollection<JobApplication>> GetByCandidateIdAsync(
        int candidateId,
        CancellationToken cancellationToken = default) =>
        (await _dbContext.Applications
            .AsNoTracking()
            .Where(application => application.CandidateId == candidateId)
            .OrderByDescending(application => application.AppliedAt)
            .ToListAsync(cancellationToken))
        .Select(application => application.ToDomain())
        .ToArray();

    public async Task<IReadOnlyCollection<JobApplication>> GetByJobIdAsync(
        int jobId,
        CancellationToken cancellationToken = default) =>
        (await _dbContext.Applications
            .AsNoTracking()
            .Where(application => application.JobId == jobId)
            .OrderByDescending(application => application.AppliedAt)
            .ToListAsync(cancellationToken))
        .Select(application => application.ToDomain())
        .ToArray();

    public async Task AddAsync(JobApplication entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbContext.Applications.Add(entity.ToRecord());

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AlreadyAppliedException(
                "Ứng viên đã ứng tuyển vào tin tuyển dụng này.");
        }
    }

    public async Task UpdateAsync(JobApplication entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var record = await _dbContext.Applications
            .SingleOrDefaultAsync(application => application.Id == entity.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Đơn ứng tuyển có mã {entity.Id} không tồn tại.");

        record.Status = entity.Status;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
