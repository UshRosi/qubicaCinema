using System.Text.Json;
using QubicaCinema.BuildingBlocks.Domain;
using QubicaCinema.BuildingBlocks.EventBus.RabbitMQ;

namespace QubicaCinema.BuildingBlocks.UnitTests.Messaging;

/// <summary>
/// The rule is "would waiting change the answer?". Getting it wrong in one direction throws away a valid
/// message; in the other it delays the ones behind a hopeless one.
/// </summary>
public sealed class EventFailureClassifierTests
{
    [Fact]
    public void Should_treat_an_unreadable_payload_as_permanent() =>
        EventFailureClassifier.IsPermanent(new JsonException("bad")).ShouldBeTrue();

    [Fact]
    public void Should_treat_data_the_model_refuses_as_permanent() =>
        EventFailureClassifier.IsPermanent(new InvalidMoneyException("no such currency")).ShouldBeTrue();

    [Fact]
    public void Should_treat_a_missing_reference_as_transient_because_it_may_still_arrive() =>
        EventFailureClassifier.IsPermanent(new MissingReferenceException()).ShouldBeFalse();

    [Fact]
    public void Should_treat_a_lost_race_as_transient() =>
        EventFailureClassifier.IsPermanent(new ConcurrentModificationException("Booking", Guid.NewGuid())).ShouldBeFalse();

    [Fact]
    public void Should_treat_an_infrastructure_failure_as_transient()
    {
        EventFailureClassifier.IsPermanent(new TimeoutException("database timed out")).ShouldBeFalse();
        EventFailureClassifier.IsPermanent(new InvalidOperationException("connection was closed")).ShouldBeFalse();
    }

    [Fact]
    public void Should_retry_what_it_does_not_recognise() =>
        EventFailureClassifier.IsPermanent(new UnrecognisedException()).ShouldBeFalse();

    private sealed class UnrecognisedException : Exception;

    private sealed class MissingReferenceException() : DomainException(DomainErrorKind.NotFound, "not announced yet");
}
