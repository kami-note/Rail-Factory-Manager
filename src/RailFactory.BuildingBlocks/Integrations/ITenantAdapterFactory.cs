namespace RailFactory.BuildingBlocks.Integrations;

public interface ITenantAdapterFactory<TPort>
{
    Task<TPort> ResolveAsync(string tenantCode, CancellationToken cancellationToken = default);
}
