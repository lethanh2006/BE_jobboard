namespace JobBoard.Domain.Entities;

public sealed class Employer : User
{
    public Employer(int id, string name, string email, Company company)
        : base(id, name, email)
    {
        ArgumentNullException.ThrowIfNull(company);

        Company = company;
    }

    public Company Company { get; }

    public int CompanyId => Company.Id;

    public override bool CanManage(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.CompanyId == CompanyId;
    }

    public override string GetDisplayName() => $"{Name} ({Company.Name})";
}
