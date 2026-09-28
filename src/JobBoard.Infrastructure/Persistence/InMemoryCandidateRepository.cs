using System.Collections.Concurrent;
using JobBoard.Application.Abstractions.Persistence;
using JobBoard.Domain.Entities;

namespace JobBoard.Infrastructure.Persistence;

public sealed class InMemoryCandidateRepository : ICandidateRepository
{
    private readonly ConcurrentDictionary<int, Candidate> _candidates = new();

    public InMemoryCandidateRepository(bool seedDemoData = false)
    {
        if (!seedDemoData)
        {
            return;
        }

        var candidate = new Candidate(
            20,
            "Nguyễn Minh An",
            "an@example.com",
            "Backend Developer yêu thích sản phẩm có tác động thực tế và những hệ thống được thiết kế chỉn chu.",
            new SalaryRange(1800, 2800));
        candidate.AddSkill(new Skill("C#"));
        candidate.AddSkill(new Skill("ASP.NET Core"));
        candidate.AddSkill(new Skill("PostgreSQL"));
        candidate.AddSkill(new Skill("Docker"));
        _candidates[candidate.Id] = candidate;
    }

    public Task<Candidate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _candidates.TryGetValue(id, out var candidate);
        return Task.FromResult(candidate);
    }

    public Task AddAsync(Candidate entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();
        if (!_candidates.TryAdd(entity.Id, entity))
        {
            throw new InvalidOperationException($"Ứng viên có mã {entity.Id} đã tồn tại.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Candidate entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();
        _candidates[entity.Id] = entity;
        return Task.CompletedTask;
    }
}
