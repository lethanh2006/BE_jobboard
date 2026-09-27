using JobBoard.Domain.Entities;

namespace JobBoard.Application.Matching;

public sealed class CompositeScorer : IJobMatchScorer
{
    private readonly IReadOnlyList<IJobMatchScorer> _scorers;

    public CompositeScorer(IEnumerable<IJobMatchScorer> scorers)
    {
        ArgumentNullException.ThrowIfNull(scorers);

        _scorers = scorers.ToList();
    }

    public decimal Score(Job job, Candidate candidate) =>
        _scorers.Sum(scorer => scorer.Score(job, candidate));
}
