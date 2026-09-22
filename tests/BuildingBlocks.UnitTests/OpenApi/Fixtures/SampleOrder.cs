namespace QubicaCinema.BuildingBlocks.UnitTests.OpenApi.Fixtures;

/// <summary>A request body with a Guid, an enum and a nested polymorphic list.</summary>
internal sealed record SampleOrder(Guid OrderId, SampleStatus Status, IReadOnlyList<SampleLine> Lines);
