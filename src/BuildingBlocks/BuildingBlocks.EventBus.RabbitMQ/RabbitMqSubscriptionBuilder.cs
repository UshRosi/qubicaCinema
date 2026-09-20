using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QubicaCinema.BuildingBlocks.Contracts;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Declares which events a service consumes and which handler reacts to each.
/// </summary>
/// <example>
/// <code>
/// services.AddRabbitMqSubscriber(connectionString, "booking", subscriptions => subscriptions
///     .Subscribe&lt;ScreeningScheduled, ScreeningScheduledHandler&gt;()
///     .Subscribe&lt;ScreeningCancelled, ScreeningCancelledHandler&gt;());
/// </code>
/// </example>
public sealed class RabbitMqSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<string, IntegrationEventDispatcher> _dispatchers = [];

    internal RabbitMqSubscriptionBuilder(IServiceCollection services) => _services = services;

    /// <summary>
    /// Routes events named <c>TEvent.Manifest</c> to <typeparamref name="THandler"/>, which is registered as
    /// a scoped service.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event is already subscribed.</exception>
    public RabbitMqSubscriptionBuilder Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent, IIntegrationEventContract
        where THandler : class, IIntegrationHandler<TEvent>
    {
        if (!_dispatchers.TryAdd(TEvent.Manifest, new IntegrationEventDispatcher<TEvent, THandler>()))
        {
            throw new InvalidOperationException(
                $"'{TEvent.Manifest}' is subscribed twice. An event has one handler per service.");
        }

        _services.TryAddScoped<THandler>();

        return this;
    }

    internal SubscriptionRegistry Build(string queueName) => new(queueName, _dispatchers);
}
