using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EngiFlow.Application.Mediation;

/// <summary>
/// Extension methods for registering EngiFlow mediation services.
/// </summary>
public static class MediationServiceCollectionExtensions
{
    /// <summary>
    /// Adds EngiFlow mediation services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to configure the mediation services.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEngiFlowMediation(this IServiceCollection services, Action<MediationConfiguration> configure)
    {
        var config = new MediationConfiguration();
        configure(config);

        services.TryAddScoped<IMediator, Mediator>();
        services.TryAddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());
        services.TryAddScoped<IPublisher>(sp => sp.GetRequiredService<IMediator>());

        foreach (var assembly in config.AssembliesToRegister)
        {
            RegisterHandlers(services, assembly, typeof(IRequestHandler<,>));
            RegisterHandlers(services, assembly, typeof(INotificationHandler<>));
        }

        foreach (var behaviorType in config.BehaviorsToRegister)
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
        }

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type openGenericInterface)
    {
        var types = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface);

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);

            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, type);
            }
        }
    }
}

/// <summary>
/// Configuration for EngiFlow mediation services.
/// </summary>
public class MediationConfiguration
{
    /// <summary>
    /// Gets the list of assemblies to scan for handlers.
    /// </summary>
    public List<Assembly> AssembliesToRegister { get; } = new();

    /// <summary>
    /// Gets the list of open-generic behavior types to register in the pipeline.
    /// </summary>
    public List<Type> BehaviorsToRegister { get; } = new();

    /// <summary>
    /// Adds an assembly to scan for request and notification handlers.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The configuration for chaining.</returns>
    public MediationConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        AssembliesToRegister.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds an open-generic behavior type to the mediation pipeline.
    /// </summary>
    /// <param name="behaviorType">The open-generic behavior type (e.g., typeof(ValidationBehavior&lt;,&gt;)).</param>
    /// <returns>The configuration for chaining.</returns>
    public MediationConfiguration AddOpenBehavior(Type behaviorType)
    {
        BehaviorsToRegister.Add(behaviorType);
        return this;
    }
}
