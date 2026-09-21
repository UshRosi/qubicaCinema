using FluentValidation;
using QubicaCinema.Identity.Api.Auth;
using QubicaCinema.Identity.Api.Tokens;

namespace QubicaCinema.Identity.Api;

/// <summary>Registers what the HTTP layer of the Identity service needs.</summary>
internal static class IdentityApiExtensions
{
    /// <summary>Adds the request validators, as singletons: they hold no state.</summary>
    internal static IServiceCollection AddIdentityValidators(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;

        services.AddSingleton<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();

        return services;
    }

    /// <summary>Adds the use cases and the token service. One line per handler, as in the other services.</summary>
    internal static IServiceCollection AddIdentityUseCases(this IServiceCollection services)
    {
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();

        // A singleton, and it may be one because it is pure: see ITokenService.
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
