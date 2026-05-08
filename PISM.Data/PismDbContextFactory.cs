using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PISM.Data;

// Used by EF Core tooling (dotnet ef migrations add / database update) at design time.
public class PismDbContextFactory : IDesignTimeDbContextFactory<PismDbContext>
{
    public PismDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PismDbContext>()
            .UseNpgsql("Host=localhost;Database=pism;Username=postgres;Password=postgres")
            .Options;

        return new PismDbContext(options);
    }
}
