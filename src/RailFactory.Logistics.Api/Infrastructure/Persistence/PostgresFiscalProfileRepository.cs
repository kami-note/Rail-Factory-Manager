using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class PostgresFiscalProfileRepository(LogisticsDbContext db) : IFiscalProfileRepository
{
    public Task<TenantFiscalProfile?> GetAsync(CancellationToken ct)
        => db.FiscalProfiles.FirstOrDefaultAsync(ct);

    public async Task UpsertAsync(TenantFiscalProfile profile, CancellationToken ct)
    {
        var existing = await db.FiscalProfiles.FindAsync(["default"], ct);
        if (existing is null)
            db.FiscalProfiles.Add(profile);
        else
            db.Entry(existing).CurrentValues.SetValues(profile);

        await db.SaveChangesAsync(ct);
    }
}
