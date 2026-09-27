using JobBoard.Domain.Entities;

namespace JobBoard.Application.Matching;

public sealed class SkillOverlapScorer : IJobMatchScorer
{
    public decimal Score(Job job, Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(candidate);

        if (job.Skills.Count == 0)
        {
            return 100;
        }

        var matchedSkillCount = job.Skills.Count(requiredSkill =>
            candidate.Skills.Any(candidateSkill =>
                string.Equals(candidateSkill.Name, requiredSkill.Name, StringComparison.OrdinalIgnoreCase)));

        return decimal.Round(matchedSkillCount * 100m / job.Skills.Count, 2);
    }
}
