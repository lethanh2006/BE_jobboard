using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobBoard.Infrastructure.Persistence.Database;

public sealed class JobBoardDbContextFactory : IDesignTimeDbContextFactory<JobBoardDbContext>
{
    public JobBoardDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<JobBoardDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=jobboard;Username=jobboard;Password=jobboard_dev_password")
            .Options;

        return new JobBoardDbContext(options);
    }
}
