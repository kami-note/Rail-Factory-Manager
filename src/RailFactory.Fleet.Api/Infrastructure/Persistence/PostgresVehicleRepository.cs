using Microsoft.EntityFrameworkCore;
using RailFactory.Fleet.Api.Application.Ports;
using RailFactory.Fleet.Api.Domain;

namespace RailFactory.Fleet.Api.Infrastructure.Persistence;

public sealed class PostgresVehicleRepository(FleetDbContext context) : IVehicleRepository
{
    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        => await context.Vehicles.AddAsync(vehicle, cancellationToken);

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.Vehicles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Vehicle?> GetByIdWithAssignmentsAsync(Guid id, CancellationToken cancellationToken)
        => context.Vehicles
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Vehicle>> ListAsync(VehicleStatus? status, CancellationToken cancellationToken)
    {
        var query = context.Vehicles.AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        return query.OrderBy(x => x.Plate).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken)
        => context.Vehicles.AnyAsync(x => x.Plate == plate, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => context.SaveChangesAsync(cancellationToken);
}
