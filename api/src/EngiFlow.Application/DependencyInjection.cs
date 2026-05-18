using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Behaviors;
using EngiFlow.Application.Mediation;
using EngiFlow.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EngiFlow.Application;

/// <summary>
/// Registers application-layer services with the dependency injection container.
/// </summary>
/// <remarks>
/// The API remains the composition root. This extension keeps CQRS dispatch,
/// validation, notifications, and use-case discovery inside the Application boundary.
/// </remarks>
public static class DependencyInjection
{
    /// <summary>
    /// Adds EngiFlow application use cases, validators, and request pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IApplicationMediator, ApplicationMediator>();
        services.AddScoped<IPostCommitNotificationQueue, PostCommitNotificationQueue>();
        services.AddScoped<IExternalOperationCompensation, ExternalOperationCompensation>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: false);
        services.AddEngiFlowMediation(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }
}
