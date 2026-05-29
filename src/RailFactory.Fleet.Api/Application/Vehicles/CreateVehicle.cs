using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Application.Vehicles;

public sealed record CreateVehicleInput(
    string Plate, string Chassis, string Renavam,
    string Type, decimal MaxWeightKg, decimal MaxVolumeCbm, DateOnly LicenseExpiry);

public sealed class CreateVehicle(IVehicleRepository repository)
{
    public async Task<Vehicle> ExecuteAsync(CreateVehicleInput input, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<VehicleType>(input.Type, ignoreCase: true, out var type))
            throw new ArgumentException($"Invalid vehicle type '{input.Type}'. Valid: {string.Join(", ", Enum.GetNames<VehicleType>())}");

        var plate = input.Plate.Trim().ToUpperInvariant();
        if (await repository.ExistsByPlateAsync(plate, cancellationToken))
            throw new InvalidOperationException($"A vehicle with plate '{plate}' already exists.");

        var vehicle = Vehicle.Create(plate, input.Chassis, input.Renavam, type,
            input.MaxWeightKg, input.MaxVolumeCbm, input.LicenseExpiry);

        await repository.AddAsync(vehicle, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return vehicle;
    }
}
