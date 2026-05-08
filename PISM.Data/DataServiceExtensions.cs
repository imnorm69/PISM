using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PISM.Data.Repositories;

namespace PISM.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddPismData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PismDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IImageRepository, ImageRepository>();
        return services;
    }
}
