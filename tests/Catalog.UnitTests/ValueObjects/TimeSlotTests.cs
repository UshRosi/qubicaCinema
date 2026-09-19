using QubicaCinema.Catalog.Domain.Exceptions;
using QubicaCinema.Catalog.Domain.ValueObjects;

namespace QubicaCinema.Catalog.UnitTests.ValueObjects;

public sealed class TimeSlotTests
{
    private static readonly DateTimeOffset Evening = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Should_reject_an_end_that_does_not_follow_the_start()
    {
        Should.Throw<InvalidTimeSlotException>(() => TimeSlot.Of(Evening, Evening));
        Should.Throw<InvalidTimeSlotException>(() => TimeSlot.Of(Evening, Evening.AddMinutes(-1)));
    }

    [Fact]
    public void Should_report_its_duration()
    {
        TimeSlot.Of(Evening, TimeSpan.FromMinutes(90)).Duration.ShouldBe(TimeSpan.FromMinutes(90));
    }

    [Fact]
    public void Should_overlap_a_slot_that_starts_inside_it()
    {
        var first = TimeSlot.Of(Evening, TimeSpan.FromMinutes(120));
        var second = TimeSlot.Of(Evening.AddMinutes(119), TimeSpan.FromMinutes(120));

        first.Overlaps(second).ShouldBeTrue();
        second.Overlaps(first).ShouldBeTrue();
    }

    [Fact]
    public void Should_overlap_a_slot_that_contains_it_entirely()
    {
        var small = TimeSlot.Of(Evening.AddMinutes(30), TimeSpan.FromMinutes(10));
        var large = TimeSlot.Of(Evening, TimeSpan.FromMinutes(120));

        small.Overlaps(large).ShouldBeTrue();
        large.Overlaps(small).ShouldBeTrue();
    }

    [Fact]
    public void Should_not_overlap_a_slot_that_starts_exactly_when_it_ends()
    {
        // Half-open intervals: this is what makes back-to-back screenings legal.
        var first = TimeSlot.Of(Evening, TimeSpan.FromMinutes(120));
        var second = TimeSlot.Of(Evening.AddMinutes(120), TimeSpan.FromMinutes(120));

        first.Overlaps(second).ShouldBeFalse();
        second.Overlaps(first).ShouldBeFalse();
    }

    [Fact]
    public void Should_contain_its_start_but_not_its_end()
    {
        var slot = TimeSlot.Of(Evening, TimeSpan.FromMinutes(120));

        slot.Contains(Evening).ShouldBeTrue();
        slot.Contains(Evening.AddMinutes(119)).ShouldBeTrue();
        slot.Contains(Evening.AddMinutes(120)).ShouldBeFalse();
    }

    [Fact]
    public void Should_have_started_from_its_first_instant()
    {
        var slot = TimeSlot.Of(Evening, TimeSpan.FromMinutes(120));

        slot.HasStartedBy(Evening.AddSeconds(-1)).ShouldBeFalse();
        slot.HasStartedBy(Evening).ShouldBeTrue();
    }

    [Fact]
    public void Should_be_equal_to_another_slot_with_the_same_ends()
    {
        TimeSlot.Of(Evening, TimeSpan.FromMinutes(90))
            .ShouldBe(TimeSlot.Of(Evening, Evening.AddMinutes(90)));
    }
}
