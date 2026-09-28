using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Application.Exceptions;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;
using JobBoard.Domain.Exceptions;

namespace JobBoard.Application.Jobs;

public sealed class JobManagementService(
    IJobRepository jobs,
    IJobApplicationRepository applications,
    IClock clock)
{
    private readonly IJobApplicationRepository _applications =
        applications ?? throw new ArgumentNullException(nameof(applications));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    private readonly IJobRepository _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));

    public async Task<IReadOnlyList<EmployerJobDto>> GetByCompanyIdAsync(
        int companyId,
        CancellationToken cancellationToken = default)
    {
        var allApplications = await _applications.GetAllAsync(cancellationToken);

        return (await _jobs.GetAllAsync(cancellationToken))
            .Where(job => job.CompanyId == companyId)
            .OrderByDescending(job => job.PostedAt)
            .Select(job => new EmployerJobDto(
                job.Id,
                job.Title,
                job.Status,
                allApplications.Count(application => application.JobId == job.Id),
                job.Deadline,
                job.PostedAt))
            .ToList();
    }

    public async Task<EmployerJobDto> CreateDraftAsync(
        CreateJobCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var job = new Job(
            await _jobs.GetNextIdAsync(cancellationToken),
            command.Title,
            command.CompanyId,
            new SalaryRange(command.SalaryMin, command.SalaryMax),
            command.Deadline,
            _clock,
            new Company(command.CompanyId, command.CompanyName, command.Location),
            command.Location,
            command.Level,
            command.WorkMode,
            command.Category,
            command.Summary,
            postedAt: _clock.UtcNow);

        foreach (var skill in command.Skills)
        {
            job.AddSkill(new Skill(skill));
        }

        await _jobs.AddAsync(job, cancellationToken);

        return new EmployerJobDto(job.Id, job.Title, job.Status, 0, job.Deadline, job.PostedAt);
    }

    public Task<EmployerJobDto> PublishAsync(
        int jobId,
        User manager,
        CancellationToken cancellationToken = default) =>
        ChangeStateAsync(jobId, manager, publish: true, cancellationToken);

    public Task<EmployerJobDto> CloseAsync(
        int jobId,
        User manager,
        CancellationToken cancellationToken = default) =>
        ChangeStateAsync(jobId, manager, publish: false, cancellationToken);

    private async Task<EmployerJobDto> ChangeStateAsync(
        int jobId,
        User manager,
        bool publish,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manager);
        var job = await _jobs.GetByIdAsync(jobId, cancellationToken)
            ?? throw new ResourceNotFoundException($"Không tìm thấy tin tuyển dụng có mã {jobId}.");

        if (!manager.CanManage(job))
        {
            throw new JobManagementForbiddenException("Người dùng không có quyền quản lý tin tuyển dụng này.");
        }

        if (publish)
        {
            job.Publish();
        }
        else
        {
            job.Close();
        }

        await _jobs.UpdateAsync(job, cancellationToken);
        var applications = await _applications.GetByJobIdAsync(job.Id, cancellationToken);

        return new EmployerJobDto(
            job.Id,
            job.Title,
            job.Status,
            applications.Count,
            job.Deadline,
            job.PostedAt);
    }
}
