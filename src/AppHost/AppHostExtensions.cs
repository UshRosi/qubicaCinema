namespace QubicaCinema.AppHost;

/// <summary>Helpers that keep <c>Program.cs</c> declarative and the gateway free of Aspire.</summary>
internal static class AppHostExtensions
{
    /// <summary>The single destination each cluster has here. Its name is part of the configuration key.</summary>
    private const string PrimaryDestination = "primary";

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
