using System.Text.Json;
using QubicaCinema.BuildingBlocks.Domain;

namespace QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

/// <summary>
/// Decides whether a failure while handling an event can be cured by trying again.
/// </summary>
/// <remarks>
/// The question is whether waiting changes the answer. A database timeout, a lost race with a concurrent
/// change, or a screening whose announcement has not arrived yet: all of these look different a few seconds
/// later, so they are retried. A payload that cannot be read, or one whose data the model refuses (a
/// currency that does not exist, a seat that is not on the grid), will fail identically forever, so it goes
/// straight to the dead-letter queue, where a person can see it, instead of spending its attempts and
/// delaying the messages behind it.
/// <para>
/// Everything not recognised as permanent is treated as transient. Wrongly retrying costs a few seconds and
/// ends in the dead-letter queue anyway; wrongly dead-lettering throws away a valid message.
/// </para>
/// </remarks>
public static class EventFailureClassifier
{
    /// <summary>Whether retrying this failure is pointless.</summary>
    public static bool IsPermanent(Exception exception) =>
        exception is JsonException
        // Invalid means the data itself is wrong. NotFound and Conflict describe the state of this
        // service at the moment, which changes: the missing screening may still arrive.
        || exception is DomainException { Kind: DomainErrorKind.Invalid };
}
