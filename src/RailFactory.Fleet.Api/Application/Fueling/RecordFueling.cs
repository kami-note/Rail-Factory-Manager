using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Fueling;

public sealed record RecordFuelingInput(
    Guid VehicleId, DateOnly Date, decimal LitersSupplied,
    decimal PricePerLiter, int? Odometer, string? Supplier, string? Notes);

public sealed class RecordFueling(IVehicleRepository vehicles, IFuelingRepository fueling)
{
    public async Task<FuelingRecord> ExecuteAsync(RecordFuelingInput input, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(input.VehicleId, ct)
            ?? throw new KeyNotFoundException($"Vehicle {input.VehicleId} not found.");

        var record = FuelingRecord.Create(
            vehicle.Id, input.Date, input.LitersSupplied,
            input.PricePerLiter, input.Odometer, input.Supplier, input.Notes);

        await fueling.SaveAsync(record, ct);
        return record;
    }
}
