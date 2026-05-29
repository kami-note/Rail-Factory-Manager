using RailFactory.Logistics.Api.Application.Ports;

namespace RailFactory.Logistics.Api.Application.Carriers;

public sealed class DeactivateCarrier(ICarrierRepository carriers)
{
    public async Task ExecuteAsync(Guid id, CancellationToken ct)
    {
        var carrier = await carriers.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Carrier {id} not found.");
        carrier.Deactivate();
        await carriers.SaveAsync(carrier, ct);
    }
}
