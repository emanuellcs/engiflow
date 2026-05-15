using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Behaviors;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Application.Mediation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EngiFlow.Application;

/// <summary>
/// Registers application-layer services with the dependency injection container.
/// </summary>
/// <remarks>
/// The API remains the composition root. This extension keeps CQRS dispatch,
/// validation, and use-case handler registration inside the Application boundary.
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

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: false);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddTransient<ICommandHandler<CreateEcoCommand, EcoDetailsDto>, CreateEcoCommandHandler>();
        services.AddTransient<ICommandHandler<SubmitEcoCommand, EcoDetailsDto>, SubmitEcoCommandHandler>();
        services.AddTransient<ICommandHandler<ApproveEcoCommand, EcoDetailsDto>, ApproveEcoCommandHandler>();
        services.AddTransient<ICommandHandler<RejectEcoCommand, EcoDetailsDto>, RejectEcoCommandHandler>();
        services.AddTransient<IQueryHandler<GetEcoByIdQuery, EcoDetailsDto>, GetEcoByIdQueryHandler>();
        services.AddTransient<IQueryHandler<ListEcosQuery, PagedResult<EcoSummaryDto>>, ListEcosQueryHandler>();

        return services;
    }
}
