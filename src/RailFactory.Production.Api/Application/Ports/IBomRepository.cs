using RailFactory.Production.Api.Domain;

namespace RailFactory.Production.Api.Application.Ports;

/// <summary>
/// Persistence port for the BillOfMaterials aggregate.
/// </summary>
public interface IBomRepository
{
    Task AddAsync(BillOfMaterials bom, CancellationToken cancellationToken);
    Task<BillOfMaterials?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<BillOfMaterials>> ListByProductCodeAsync(string productCode, CancellationToken cancellationToken);
    Task<List<BillOfMaterials>> ListAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the currently Active BOM for the given product, or null if none exists.
    /// </summary>
    Task<BillOfMaterials?> GetActiveByProductCodeAsync(string productCode, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the highest version number registered for a product, or 0 if none.
    /// Used to auto-increment versions on creation.
    /// </summary>
    Task<int> GetLatestVersionNumberAsync(string productCode, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Directly inserts a BomItem and updates the parent BOM's UpdatedAt via raw SQL,
    /// bypassing EF Core change tracking to avoid Npgsql ValueGeneratedOnAdd conflicts.
    /// </summary>
    Task AddItemDirectAsync(Guid bomId, BomItem item, DateTimeOffset bomUpdatedAt, CancellationToken cancellationToken);
}
