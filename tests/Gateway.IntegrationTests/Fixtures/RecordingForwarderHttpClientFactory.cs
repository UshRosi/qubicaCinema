using Yarp.ReverseProxy.Forwarder;

namespace QubicaCinema.Gateway.IntegrationTests.Fixtures;

/// <summary>
/// Hands YARP a client that talks to the recording stub instead of the network. YARP resolves this from the
/// container for every cluster, which is what makes the real routing observable without a real service.
/// </summary>
internal sealed class RecordingForwarderHttpClientFactory(RecordingHandler handler) : IForwarderHttpClientFactory
{
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context) =>
        new(handler, disposeHandler: false);
}
