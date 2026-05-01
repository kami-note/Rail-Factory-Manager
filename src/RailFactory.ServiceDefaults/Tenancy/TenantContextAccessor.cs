namespace Microsoft.Extensions.Hosting;

internal sealed class TenantContextAccessor : ITenantContextAccessor
{
    public RailFactory.BuildingBlocks.Tenancy.TenantContext? Current { get; set; }
}
