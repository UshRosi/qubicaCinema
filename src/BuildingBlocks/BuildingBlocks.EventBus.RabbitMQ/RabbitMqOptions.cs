using System.ComponentModel.DataAnnotations;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// The topology and delivery settings of the bus. The broker's address is not here: it is a connection
/// string, supplied by the environment, while these are properties of how the services talk.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>The exchange every service publishes to. Publishers and consumers must agree on it.</summary>
    public const string DefaultExchangeName = "qubica.events";

    /// <summary>The topic exchange events are published to, with the event's wire name as routing key.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ExchangeName { get; set; } = DefaultExchangeName;

    /// <summary>
    /// How many unacknowledged messages the broker hands a consumer at once. The consumer works through
    /// them one at a time, so a larger value only saves round trips.
    /// </summary>
    [Range(1, 1000)]
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// How long a message that failed for a transient reason waits before its next attempt, one entry per
    /// retry. The defaults give a message four attempts in all — the first, then one after 5 seconds, one after
    /// 30 seconds and one after 2 minutes — which rides out a database restart or a deployment. When the
    /// list runs out the message goes to the dead-letter queue.
    /// </summary>
    [Required]
    public IReadOnlyList<TimeSpan> RetryDelays { get; set; } = [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2)];
}
