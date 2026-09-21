using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using QubicaCinema.BuildingBlocks.Application.Security;

namespace QubicaCinema.BuildingBlocks.Authentication;

/// <summary>Registers bearer validation, the policies and the current user.</summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Validates bearer tokens against the shared <see cref="JwtOptions"/>, and fails at startup if they are unusable.
    /// </summary>
    /// <remarks>
    /// The section is a parameter, not something this method fishes out of <c>IConfiguration</c>, for the same
    /// reason as the connection strings: a test, or a second host, must be able to hand it any value.
    /// </remarks>
    public static IServiceCollection AddCinemaAuthentication(this IServiceCollection services, IConfigurationSection jwtSection)
    {
        services.AddJwtOptions(jwtSection);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>());

        return services;
    }

    /// <summary>Registers <see cref="JwtOptions"/> and its validation, without bearer validation. What an issuer needs.</summary>
    public static IServiceCollection AddJwtOptions(this IServiceCollection services, IConfigurationSection jwtSection)
    {
        services.AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .ValidateDataAnnotations()
            // A short or missing key stops the process while it starts, with a sentence that names the fix,
            // instead of surfacing as a 401 on the first request that says nothing.
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>());

        return services;
    }

    /// <summary>Adds the <see cref="CinemaPolicies"/>.</summary>
    public static IServiceCollection AddCinemaAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(CinemaPolicies.Admin, policy => policy.RequireRole(CinemaRoles.Admin))
            .AddPolicy(CinemaPolicies.Customer, policy => policy.RequireRole(CinemaRoles.Customer))
            .AddPolicy(CinemaPolicies.Authenticated, policy => policy.RequireAuthenticatedUser());

        return services;
    }

    /// <summary>Makes <see cref="ICurrentUser"/> the caller named by the validated token.</summary>
    public static IServiceCollection AddCinemaCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, ClaimsPrincipalCurrentUser>();

        return services;
    }
}
