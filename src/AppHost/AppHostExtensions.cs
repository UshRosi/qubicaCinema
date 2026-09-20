namespace QubicaCinema.AppHost;

/// <summary>Helpers that keep <c>Program.cs</c> declarative and the gateway free of Aspire.</summary>
internal static class AppHostExtensions
{
    /// <summary>The single destination each cluster has here. Its name is part of the configuration key.</summary>
    private const string PrimaryDestination = "primary";

    /// <summary>Who issues the tokens. Only an identifier, never dereferenced, so it need not be reachable.</summary>
    private const string JwtIssuer = "https://qubicacinema.local/identity";

    /// <summary>Who the tokens are for. A token minted for another API is rejected by every validator.</summary>
    private const string JwtAudience = "qubicacinema-api";

    /// <summary>
    /// Hands a project the three values that make its tokens agree with everyone else's: the issuer, the
    /// audience and the one shared signing key.
    /// </summary>
    /// <remarks>
    /// Applied to Identity, Catalog, Booking and the gateway. Plain <c>Jwt__*</c> keys, so outside Aspire the
    /// same four processes are configured by setting the same three environment variables.
    /// </remarks>
    internal static IResourceBuilder<TProject> WithJwt<TProject>(
        this IResourceBuilder<TProject> project,
        IResourceBuilder<ParameterResource> signingKey)
        where TProject : IResourceWithEnvironment =>
        project
            .WithEnvironment("Jwt__Issuer", JwtIssuer)
            .WithEnvironment("Jwt__Audience", JwtAudience)
            .WithEnvironment("Jwt__SigningKey", signingKey);

    /// <summary>
    /// Points one YARP cluster at a service by writing the configuration key the gateway already declares.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>WithReference</c>: that injects <c>services__catalog__http__0</c> for
    /// Microsoft.Extensions.ServiceDiscovery, which this solution does not use, and the gateway would carry a
    /// key it never reads. The key it does read is
    /// <c>ReverseProxy__Clusters__{clusterId}__Destinations__primary__Address</c>, the one its own
    /// <c>appsettings.json</c> declares, so the difference between Aspire and a Helm chart is only who sets
    /// the variable.
    /// </remarks>
    internal static IResourceBuilder<TGateway> WithProxyDestination<TGateway, TService>(
        this IResourceBuilder<TGateway> gateway,
        string clusterId,
        IResourceBuilder<TService> service,
        string endpointName = "http")
        where TGateway : IResourceWithEnvironment
        where TService : IResourceWithEndpoints =>
        gateway.WithEnvironment(
            $"ReverseProxy__Clusters__{clusterId}__Destinations__{PrimaryDestination}__Address",
            service.GetEndpoint(endpointName));
}
