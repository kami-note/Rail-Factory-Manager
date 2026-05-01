namespace Microsoft.Extensions.Hosting;

public static class ServiceDefaultsKeys
{
    public const string HealthEndpointPath = "/health";
    public const string AliveEndpointPath = "/alive";
    public const string CorrelationIdHeaderName = "X-Correlation-Id";
}
