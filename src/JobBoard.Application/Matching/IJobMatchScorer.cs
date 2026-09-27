using JobBoard.Domain.Entities;

namespace JobBoard.Application.Matching;

public interface IJobMatchScorer
{
    decimal Score(Job job, Candidate candidate);
}
