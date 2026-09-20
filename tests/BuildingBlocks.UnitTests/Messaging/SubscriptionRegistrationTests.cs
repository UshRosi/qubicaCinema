using Microsoft.Extensions.DependencyInjection;
using QubicaCinema.BuildingBlocks.Contracts.Catalog;
using QubicaCinema.BuildingBlocks.EventBus;
using QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

public sealed class SubscriptionRegistrationTests
{
    private const string Amqp = "amqp://guest:guest@localhost:5672";

    [Fact]
    public void Should_register_each_handler_as_a_scoped_service()
    {
        var services = new ServiceCollection();

        services.AddRabbitMqSubscriber(Amqp, "test", subscriptions => subscriptions
            .Subscribe<ScreeningCancelled, CancelledHandler>());

        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(CancelledHandler) && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Should_refuse_two_handlers_for_one_event()
    {
        var services = new ServiceCollection();

        var exception = Should.Throw<InvalidOperationException>(() =>
            services.AddRabbitMqSubscriber(Amqp, "test", subscriptions => subscriptions
                .Subscribe<ScreeningCancelled, CancelledHandler>()
                .Subscribe<ScreeningCancelled, OtherCancelledHandler>()));

        exception.Message.ShouldContain(ScreeningCancelled.Manifest);
    }

    [Fact]
    public void Should_refuse_a_queue_without_a_name() =>
        Should.Throw<ArgumentException>(() =>
            new ServiceCollection().AddRabbitMqSubscriber(Amqp, " ", _ => { }));

    [Fact]
    public void Should_register_the_connection_once_when_a_service_publishes_and_subscribes()
    {
        var services = new ServiceCollection();

        services.AddRabbitMqEventBus(Amqp);
        services.AddRabbitMqSubscriber(Amqp, "test", subscriptions => subscriptions
            .Subscribe<ScreeningCancelled, CancelledHandler>());

        services.Count(descriptor => descriptor.ServiceType == typeof(RabbitMqConnectionProvider)).ShouldBe(1);
    }

    private sealed class CancelledHandler : IIntegrationHandler<ScreeningCancelled>
    {
        public Task HandleAsync(ScreeningCancelled integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OtherCancelledHandler : IIntegrationHandler<ScreeningCancelled>
    {
        public Task HandleAsync(ScreeningCancelled integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
