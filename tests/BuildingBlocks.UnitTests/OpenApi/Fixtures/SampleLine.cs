using System.Text.Json.Serialization;

namespace QubicaCinema.BuildingBlocks.UnitTests.OpenApi.Fixtures;

/// <summary>A polymorphic value with a <c>mode</c> discriminator, shaped like the booking's seat selection.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(SampleLineByCode), "code")]
[JsonDerivedType(typeof(SampleLineByQuantity), "quantity")]
internal abstract record SampleLine;
