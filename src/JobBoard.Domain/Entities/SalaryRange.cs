namespace JobBoard.Domain.Entities;

public sealed record SalaryRange
{
    public SalaryRange(decimal min, decimal max)
    {
        if (min < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "Mức lương tối thiểu không được âm.");
        }

        if (min > max)
        {
            throw new ArgumentException("Mức lương tối thiểu không được lớn hơn mức lương tối đa.", nameof(max));
        }

        Min = min;
        Max = max;
    }

    public decimal Min { get; }

    public decimal Max { get; }

    public bool Overlaps(SalaryRange other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Min <= other.Max && other.Min <= Max;
    }
}
