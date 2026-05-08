using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PISM.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddPismData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PismDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
