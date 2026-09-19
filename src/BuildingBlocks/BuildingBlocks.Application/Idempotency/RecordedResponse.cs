namespace QubicaCinema.BuildingBlocks.Application.Idempotency;

/// <summary>What the first execution of an idempotent request answered, kept so a retry can be given it again.</summary>
/// <param name="StatusCode">The status code the first execution returned.</param>
/// <param name="Body">The JSON body it returned.</param>
public sealed record RecordedResponse(int StatusCode, string Body);
