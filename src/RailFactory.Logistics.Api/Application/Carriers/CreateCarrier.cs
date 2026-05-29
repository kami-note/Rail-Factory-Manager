using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Application.Carriers;

public sealed record CreateCarrierInput(
    string Name, string DocumentNumber, string? ContactEmail,
    decimal RatePerKg, decimal RatePerCbm);

public sealed class CreateCarrier(ICarrierRepository carriers)
{
    public async Task<Carrier> ExecuteAsync(CreateCarrierInput input, CancellationToken ct)
    {
        var carrier = Carrier.Create(input.Name, input.DocumentNumber, input.ContactEmail,
            input.RatePerKg, input.RatePerCbm);
        await carriers.SaveAsync(carrier, ct);
        return carrier;
    }
}
