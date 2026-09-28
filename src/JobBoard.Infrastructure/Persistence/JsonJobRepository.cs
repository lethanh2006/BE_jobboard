using System.Text.Json;
using System.Text.Json.Serialization;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence;

public sealed class JsonJobRepository : IJobRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IClock _clock;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly object _syncRoot = new();
    private readonly string _seedFilePath;
    private Dictionary<int, Job>? _jobs;

    public JsonJobRepository(string seedFilePath, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(seedFilePath))
        {
            throw new ArgumentException("Đường dẫn dữ liệu seed không được để trống.", nameof(seedFilePath));
        }

        _seedFilePath = seedFilePath;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<Job?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        lock (_syncRoot)
        {
            return _jobs!.GetValueOrDefault(id);
        }
    }

    public async Task<IReadOnlyCollection<Job>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        lock (_syncRoot)
        {
            return _jobs!.Values.ToArray();
        }
    }

    public async Task AddAsync(Job entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await EnsureLoadedAsync(cancellationToken);

        lock (_syncRoot)
        {
            _jobs!.Add(entity.Id, entity);
        }
    }

    public async Task UpdateAsync(Job entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await EnsureLoadedAsync(cancellationToken);

        lock (_syncRoot)
        {
            _jobs![entity.Id] = entity;
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_jobs is not null)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_jobs is not null)
            {
                return;
            }

            await using var stream = File.OpenRead(_seedFilePath);
            var seeds = await JsonSerializer.DeserializeAsync<List<JobSeed>>(
                stream,
                SerializerOptions,
                cancellationToken) ?? [];

            _jobs = seeds
                .Select(MapJob)
                .ToDictionary(job => job.Id);
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private Job MapJob(JobSeed seed)
    {
        var job = new Job(
            seed.Id,
            seed.Title,
            seed.CompanyId,
            new SalaryRange(seed.SalaryMin, seed.SalaryMax),
            seed.Deadline,
            _clock,
            new Company(seed.CompanyId, seed.Company, seed.Location),
            seed.Location,
            seed.Level,
            seed.WorkMode,
            seed.Category,
            seed.Summary,
            seed.Description,
            seed.Requirements,
            seed.Benefits,
            seed.PostedAt,
            seed.Featured);

        foreach (var skill in seed.Skills)
        {
            job.AddSkill(new Skill(skill));
        }

        if (seed.Status is JobStatus.Published or JobStatus.Closed)
        {
            job.Publish();
        }

        if (seed.Status == JobStatus.Closed)
        {
            job.Close();
        }

        return job;
    }

    private sealed record JobSeed(
        int Id,
        string Title,
        int CompanyId,
        string Company,
        string Location,
        decimal SalaryMin,
        decimal SalaryMax,
        JobLevel Level,
        WorkMode WorkMode,
        string Category,
        string Summary,
        IReadOnlyList<string> Description,
        IReadOnlyList<string> Requirements,
        IReadOnlyList<string> Benefits,
        DateTime PostedAt,
        DateTime Deadline,
        JobStatus Status,
        bool Featured,
        IReadOnlyList<string> Skills);
}
