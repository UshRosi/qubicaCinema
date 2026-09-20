using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using QubicaCinema.BuildingBlocks.Contracts;

namespace QubicaCinema.BuildingBlocks.EventBus;

/// <summary>
/// The one place integration events are turned into JSON and back, so that the writer and the reader
/// cannot disagree about the format.
/// </summary>
public static class IntegrationEventSerializer
{
    /// <summary>
    /// Web defaults: camelCase names and case-insensitive reading. <c>UnmappedMemberHandling</c> stays at
    /// its default of skipping unknown properties — setting it to <c>Disallow</c> would let a new field in a
    /// publisher take down every consumer that has not been redeployed yet.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        // The wire name travels beside the payload, as the routing key, and not inside it. It is ignored here,
        // once, rather than by an attribute on every event: System.Text.Json does not carry an attribute from
        // an abstract property over to its override, so a new event that forgot one would leak the name.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoreEventName },
        },
    };

    /// <summary>Serializes an event by its runtime type, so a derived record is never written as its base.</summary>
    public static string Serialize(IntegrationEvent integrationEvent) =>
        JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Options);

    /// <summary>Reads an event of a known type.</summary>
    /// <exception cref="JsonException">The payload is not a valid <typeparamref name="TEvent"/>.</exception>
    public static TEvent Deserialize<TEvent>(string payload)
        where TEvent : IntegrationEvent =>
        JsonSerializer.Deserialize<TEvent>(payload, Options)
        ?? throw new JsonException($"The payload of a {typeof(TEvent).Name} was the JSON value null.");

    private static void IgnoreEventName(JsonTypeInfo typeInfo)
    {
        if (!typeof(IntegrationEvent).IsAssignableFrom(typeInfo.Type))
        {
            return;
        }

        for (int index = typeInfo.Properties.Count - 1; index >= 0; index--)
        {
            if (typeInfo.Properties[index].Name.Equals(nameof(IntegrationEvent.EventName), StringComparison.OrdinalIgnoreCase))
            {
                typeInfo.Properties.RemoveAt(index);
            }
        }
    }
}
