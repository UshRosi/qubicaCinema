namespace QubicaCinema.BuildingBlocks.Application.Idempotency;

/// <summary>One request that a client has asked to be carried out at most once.</summary>
/// <param name="UserId">
/// Whose key it is. Keys are scoped by user, so two clients that happen to pick the same key cannot collide,
/// and nobody can probe for another user's keys.
/// </param>
/// <param name="Key">The value of the <c>Idempotency-Key</c> header.</param>
/// <param name="RequestHash">
/// A fingerprint of the request body, so that a key reused for a <em>different</em> request is recognised
/// as a client bug instead of silently answered with the first request's response.
/// </param>
public sealed record IdempotentRequest(Guid UserId, string Key, string RequestHash);
