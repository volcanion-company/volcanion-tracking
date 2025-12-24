using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VolcanionTracking.Application.Common.Interfaces;
using VolcanionTracking.Infrastructure.Persistence;
using VolcanionTracking.Infrastructure.Persistence.Repositories;
using VolcanionTracking.Infrastructure.Services;

namespace VolcanionTracking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Database contexts
        services.AddDbContext<WriteDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("WriteDatabase"),
                b => b.MigrationsAssembly(typeof(WriteDbContext).Assembly.FullName)));

        services.AddDbContext<ReadDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ReadDatabase"),
                b => b.MigrationsAssembly(typeof(ReadDbContext).Assembly.FullName)));

        // UnitOfWork
        services.AddScoped<IUnitOfWork>(provider => 
            provider.GetRequiredService<WriteDbContext>());

        // Repositories
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<IPartnerSystemRepository, PartnerSystemRepository>();
        services.AddScoped<ITrackingEventRepository, TrackingEventRepository>();
        services.AddScoped<ITrackingEventReadRepository, TrackingEventReadRepository>();

        // Redis cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "VolcanionTracking:";
        });

        // Services
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IEventValidationService, EventValidationService>();

        return services;
    }
}
