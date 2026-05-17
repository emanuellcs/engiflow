using EngiFlow.Application.Abstractions.Cqrs;
using EngiFlow.Application.Auth.Dtos;
using EngiFlow.Application.Auth.Commands;
using EngiFlow.Application.Auth.Queries;
using EngiFlow.Application.Behaviors;
using EngiFlow.Application.Ecos.Commands;
using EngiFlow.Application.Ecos.Dtos;
using EngiFlow.Application.Ecos.Queries;
using EngiFlow.Application.Mediation;
using EngiFlow.Application.Users.Commands;
using EngiFlow.Application.Users.Dtos;
using EngiFlow.Application.Users.Queries;
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
        services.AddTransient<ICommandHandler<CreateUserCommand, UserSummaryDto>, CreateUserCommandHandler>();
        services.AddTransient<ICommandHandler<ForgotPasswordCommand, ForgotPasswordResultDto>, ForgotPasswordCommandHandler>();
        services.AddTransient<ICommandHandler<RegisterCompanyCommand, LoginResultDto>, RegisterCompanyCommandHandler>();
        services.AddTransient<IQueryHandler<LoginQuery, LoginResultDto>, LoginQueryHandler>();
        services.AddTransient<IQueryHandler<GetEcoByIdQuery, EcoDetailsDto>, GetEcoByIdQueryHandler>();
        services.AddTransient<IQueryHandler<ListEcosQuery, PagedResult<EcoSummaryDto>>, ListEcosQueryHandler>();
        services.AddTransient<IQueryHandler<ListUsersQuery, IReadOnlyList<UserSummaryDto>>, ListUsersQueryHandler>();

        return services;
    }
}
