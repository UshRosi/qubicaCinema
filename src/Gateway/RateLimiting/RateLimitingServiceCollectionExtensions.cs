using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace QubicaCinema.Gateway.RateLimiting;

/// <summary>Registers the gateway's rate limiter and the options it reads.</summary>
internal static class RateLimitingServiceCollectionExtensions
{
    internal static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            // A nonsensical limit is a legible failure at startup, not a gateway that quietly lets everything
            // through until somebody notices.
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RateLimitOptions>, RateLimitOptionsValidator>());

        services.AddRateLimiter();
        services.AddSingleton<IConfigureOptions<RateLimiterOptions>, ConfigureGatewayRateLimiter>();

        return services;
    }
}
