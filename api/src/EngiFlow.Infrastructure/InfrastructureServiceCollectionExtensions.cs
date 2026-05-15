using EngiFlow.Application.Abstractions.Persistence;
using EngiFlow.Application.Abstractions.Security;
using EngiFlow.Infrastructure.Persistence;
using EngiFlow.Infrastructure.Persistence.Interceptors;
using EngiFlow.Infrastructure.Persistence.Repositories;
using EngiFlow.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EngiFlow.Infrastructure;

/// <summary>
/// Registers infrastructure services with the application dependency injection container.
/// </summary>
/// <remarks>
/// The API project remains the composition root, but keeping persistence registration in
/// Infrastructure prevents startup code from knowing EF Core interceptor and migration
/// details. This is the same boundary future storage adapters and integration services
/// should use.
/// </remarks>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds the EngiFlow PostgreSQL DbContext and persistence interceptors.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A PostgreSQL connection string is required.", nameof(connectionString));
        }

        services.AddScoped<EcoAuditSaveChangesInterceptor>();
        services.AddScoped<IEngineeringChangeOrderRepository, EngineeringChangeOrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHashService, AspNetCorePasswordHashService>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddDbContext<EngiFlowDbContext>((serviceProvider, options) =>
        {
            options
                .UseNpgsql(
                    connectionString,
                    npgsql => npgsql.MigrationsAssembly(typeof(EngiFlowDbContext).Assembly.FullName))
                .AddInterceptors(serviceProvider.GetRequiredService<EcoAuditSaveChangesInterceptor>());
        });

        return services;
    }
}
