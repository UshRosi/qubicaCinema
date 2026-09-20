namespace QubicaCinema.Gateway;

/// <summary>Telemetry names the gateway subscribes to but does not own.</summary>
public static class GatewayDiagnostics
{
    /// <summary>The name YARP gives both its <see cref="System.Diagnostics.ActivitySource"/> and its meter.</summary>
    public const string ReverseProxy = "Yarp.ReverseProxy";
}
