using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>Registers the RabbitMQ event bus, and the consumer of a service that subscribes.</summary>
/// <remarks>
/// The connection string is a parameter, not read from <c>IConfiguration</c> in here, for the reason given in
/// the persistence extensions: a composition root that reads configuration behind the caller's back cannot
/// be used from a test or from a host that got the value from somewhere else.
/// </remarks>
public static class RabbitMqServiceCollectionExtensions
{
    /// <summary>The name of the connection string, and of the health check that watches it.</summary>
    public const string ConnectionName = "rabbitmq";

    /// <summary>Registers the publisher: an <see cref="IEventBus"/> over one channel.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The broker's AMQP URI.</param>
    /// <param name="configure">Optional overrides of <see cref="RabbitMqOptions"/>.</param>
    public static IServiceCollection AddRabbitMqEventBus(
        this IServiceCollection services,
        string connectionString,
        Action<RabbitMqOptions>? configure = null)
    {
        services.AddRabbitMqCore(connectionString, configure);
        services.TryAddSingleton<IEventBus, RabbitMqEventBus>();

        return services;
    }

    /// <summary>Registers the consumer of a service: its queue, and the handler for each event it subscribes to.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The broker's AMQP URI.</param>
    /// <param name="queueName">The service's own queue. One queue per service, so each gets its own copy of every event.</param>
    /// <param name="subscribe">Declares the events and their handlers.</param>
    /// <param name="configure">Optional overrides of <see cref="RabbitMqOptions"/>.</param>
    public static IServiceCollection AddRabbitMqSubscriber(
        this IServiceCollection services,
        string connectionString,
        string queueName,
        Action<RabbitMqSubscriptionBuilder> subscribe,
        Action<RabbitMqOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        services.AddRabbitMqCore(connectionString, configure);

        var builder = new RabbitMqSubscriptionBuilder(services);
        subscribe(builder);

        services.AddSingleton(builder.Build(queueName));
        services.AddHostedService<RabbitMqConsumerService>();

        return services;
    }

    private static void AddRabbitMqCore(
        this IServiceCollection services,
        string connectionString,
        Action<RabbitMqOptions>? configure)
    {
        // A service that publishes and subscribes calls both extensions; the core is registered once.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(RabbitMqConnectionProvider)))
        {
            return;
        }

        services.AddOptions<RabbitMqOptions>()
            .Configure(configure ?? (_ => { }))
            .ValidateDataAnnotations()
            // A missing value is a legible failure at startup, not a NullReferenceException on the first message.
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>());
        services.AddSingleton(provider => new RabbitMqRetryPolicy(
            provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value.RetryDelays));

        services.AddSingleton(provider => new RabbitMqConnectionProvider(
            connectionString,
            provider.GetRequiredService<ILogger<RabbitMqConnectionProvider>>()));

        services.AddHealthChecks()
            .AddCheck<RabbitMqHealthCheck>(ConnectionName, failureStatus: HealthStatus.Degraded, tags: ["ready"]);
    }
}
