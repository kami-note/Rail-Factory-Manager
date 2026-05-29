using Microsoft.EntityFrameworkCore;
using RailFactory.Logistics.Api.Application.Ports;
using RailFactory.Logistics.Api.Domain;

namespace RailFactory.Logistics.Api.Infrastructure.Persistence;

public sealed class PostgresShipmentOrderRepository(LogisticsDbContext db) : IShipmentOrderRepository
{
    public Task<ShipmentOrder?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.ShipmentOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<ShipmentOrder>> ListAsync(ShipmentOrderStatus? status, CancellationToken ct)
    {
        var query = db.ShipmentOrders.Include(x => x.Items).AsQueryable();
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
    }

    public async Task SaveAsync(ShipmentOrder order, CancellationToken ct)
    {
        if (db.Entry(order).State == EntityState.Detached)
            db.ShipmentOrders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddItemDirectAsync(Guid orderId, ShipmentItem item, DateTimeOffset orderUpdatedAt, CancellationToken ct)
    {
        // Bypass EF Core change tracking to avoid the EF Core 10 + Npgsql 10 bug where
        // new entities with a non-zero Guid PK are treated as Unchanged (ValueGeneratedOnAdd
        // convention), causing SaveChanges to generate an UPDATE on a non-existent row.
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "shipment_items" ("Id", "ShipmentOrderId", "MaterialCode", "Quantity", "UnitOfMeasure", "WeightKg", "VolumeCbm")
            VALUES ({item.Id}, {orderId}, {item.MaterialCode}, {item.Quantity}, {item.UnitOfMeasure}, {item.WeightKg}, {item.VolumeCbm})
            """,
            ct);

        await db.Database.ExecuteSqlAsync(
            $"""
            UPDATE "shipment_orders" SET "UpdatedAt" = {orderUpdatedAt} WHERE "Id" = {orderId}
            """,
            ct);
    }
}
