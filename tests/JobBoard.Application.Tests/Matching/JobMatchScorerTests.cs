using JobBoard.Application.Matching;
using JobBoard.Domain.Abstractions;
using JobBoard.Domain.Entities;

namespace JobBoard.Application.Tests.Matching;

public sealed class JobMatchScorerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SkillOverlapScorer_ReturnsMatchedSkillPercentage()
    {
        var job = CreateJob(new SalaryRange(1000, 2000), "C#", "PostgreSQL");
        var candidate = CreateCandidate();
        candidate.AddSkill(new Skill("c#"));
        candidate.AddSkill(new Skill("TypeScript"));
        var scorer = new SkillOverlapScorer();

        var score = scorer.Score(job, candidate);

        Assert.Equal(50, score);
    }

    [Fact]
    public void SalaryFitScorer_WhenRangesOverlap_ReturnsFullScore()
    {
        var job = CreateJob(new SalaryRange(1000, 2000));
        var candidate = CreateCandidate(new SalaryRange(1800, 2500));
        var scorer = new SalaryFitScorer();

        var score = scorer.Score(job, candidate);

        Assert.Equal(100, score);
    }

    [Fact]
    public void SalaryFitScorer_WhenCandidateHasNoExpectation_ReturnsZero()
    {
        var scorer = new SalaryFitScorer();

        var score = scorer.Score(CreateJob(new SalaryRange(1000, 2000)), CreateCandidate());

        Assert.Equal(0, score);
    }

    [Fact]
    public void CompositeScorer_SumsScoresFromChildScorers()
    {
        var job = CreateJob(new SalaryRange(1000, 2000), "C#", "PostgreSQL");
        var candidate = CreateCandidate(new SalaryRange(1500, 2500));
        candidate.AddSkill(new Skill("C#"));
        var scorer = new CompositeScorer([new SkillOverlapScorer(), new SalaryFitScorer()]);

        var score = scorer.Score(job, candidate);

        Assert.Equal(150, score);
    }

    private static Candidate CreateCandidate(SalaryRange? desiredSalary = null) =>
        new(1, "Nguyễn An", "an@example.com", desiredSalary: desiredSalary);

    private static Job CreateJob(SalaryRange salary, params string[] skills)
    {
        var job = new Job(
            1,
            "Backend Developer",
            30,
            salary,
            Now.AddDays(7),
            new FixedClock(Now));

        foreach (var skill in skills)
        {
            job.AddSkill(new Skill(skill));
        }

        return job;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
