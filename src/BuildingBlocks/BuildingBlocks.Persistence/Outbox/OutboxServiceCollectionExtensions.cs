using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.Persistence.Outbox;

/// <summary>Registers the outbox publisher for a service's <c>DbContext</c>.</summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Adds the outbox processor and, unless told otherwise, the background service that runs it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="runPublisher">
    /// Whether to start the publishing loop. An integration test passes <c>false</c>, resolves
    /// <see cref="IOutboxProcessor"/> and pumps it by hand, which is deterministic; removing a hosted
    /// service from a test host is not.
    /// </param>
    /// <param name="configure">Optional overrides of <see cref="OutboxOptions"/>.</param>
    /// <typeparam name="TContext">The service's <c>DbContext</c>, which owns the outbox table.</typeparam>
    public static IServiceCollection AddOutboxPublisher<TContext>(
        this IServiceCollection services,
        bool runPublisher = true,
        Action<OutboxOptions>? configure = null)
        where TContext : DbContext
    {
        services.AddOptions<OutboxOptions>()
            .Configure(configure ?? (_ => { }))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<OutboxOptions>, OutboxOptionsValidator>());

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IOutboxProcessor, OutboxProcessor<TContext>>();

        if (runPublisher)
        {
            services.AddHostedService<OutboxPublisherBackgroundService>();
        }

        return services;
    }
}
