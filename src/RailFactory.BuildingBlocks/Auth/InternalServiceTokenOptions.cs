namespace RailFactory.BuildingBlocks.Auth;

public sealed class InternalServiceTokenOptions
{
    public const string SectionName = "InternalToken";

    public string Issuer { get; set; } = "railfactory.frontend";

    public string Audience { get; set; } = "railfactory.internal";

    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 5;
}

public static class InternalServiceTokenClaimTypes
{
    public const string Tenant = "tenant";
    public const string Permission = "permission";
}
