using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PISM.Core.Services;
using PISM.Data.Repositories;
using PISM.Data.Services;

namespace PISM.Data;

public static class DataServiceExtensions
{
    public static IServiceCollection AddPismData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PismDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IImageRepository, ImageRepository>();
        services.AddScoped<IScanJobRepository, ScanJobRepository>();
        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<ResetService>();
        services.AddScoped<ReviewService>();
        return services;
    }
}
