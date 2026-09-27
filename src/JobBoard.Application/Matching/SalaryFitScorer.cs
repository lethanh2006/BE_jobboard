using JobBoard.Domain.Entities;

namespace JobBoard.Application.Matching;

public sealed class SalaryFitScorer : IJobMatchScorer
{
    public decimal Score(Job job, Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(candidate);

        return candidate.DesiredSalary is not null && job.Salary.Overlaps(candidate.DesiredSalary)
            ? 100
            : 0;
    }
}
