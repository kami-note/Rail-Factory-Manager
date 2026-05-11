using Microsoft.EntityFrameworkCore;
using RailFactory.BuildingBlocks.Tenancy;
using RailFactory.Inventory.Api.Application.Ports;
using RailFactory.Inventory.Api.Domain;

namespace RailFactory.Inventory.Api.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL implementation of the Material repository.
/// </summary>
public sealed class PostgresMaterialRepository(InventoryDbContext dbContext) : IMaterialRepository
{
    public Task<Material?> GetByCodeAsync(string materialCode, CancellationToken cancellationToken)
    {
        var code = MaterialCode.From(materialCode);
        return dbContext.Materials
            .FirstOrDefaultAsync(x => x.MaterialCode == code, cancellationToken);
    }

    public Task<Material?> GetByGtinAsync(string gtin, CancellationToken cancellationToken)
    {
        var normalizedGtin = gtin.Trim();
        return dbContext.Materials
            .FirstOrDefaultAsync(x => x.Gtin == normalizedGtin, cancellationToken);
    }

    public Task<Dictionary<string, Material>> GetByCodesAsync(IEnumerable<string> materialCodes, CancellationToken cancellationToken)
    {
        var codes = materialCodes.Select(MaterialCode.From).ToList();
        return dbContext.Materials
            .Where(x => codes.Contains(x.MaterialCode))
            .ToDictionaryAsync(x => x.MaterialCode.Value, cancellationToken);
    }

    public async Task AddAsync(Material material, CancellationToken cancellationToken)
    {
        await dbContext.Materials.AddAsync(material, cancellationToken);
    }

    public Task<List<Material>> SearchAsync(string term, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Task.FromResult(new List<Material>());
        }

        var normalizedTerm = $"%{term.Trim().ToLowerInvariant()}%";

        // ELITE FIX: Do NOT use .Value inside ILike for Value Objects with HasConversion.
        // EF Core needs to see the property as its domain type for the translator to work.
        // However, for string-based partial matches (ILIKE), we cast to (string)(object) 
        // or rely on the fact that EF.Functions.ILike expects a string.
        
        return dbContext.Materials
            .Where(x => EF.Functions.ILike((string)(object)x.MaterialCode, normalizedTerm) ||
                        EF.Functions.ILike(x.OfficialName, normalizedTerm) ||
                        (x.Gtin != null && EF.Functions.ILike(x.Gtin, normalizedTerm)))
            .OrderBy(x => x.OfficialName)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertSupplierMaterialHintAsync(SupplierMaterialHint hint, CancellationToken cancellationToken)
    {
        var existing = await dbContext.SupplierMaterialHints
            .FirstOrDefaultAsync(x => x.SupplierFiscalId == hint.SupplierFiscalId && x.SupplierProductCode == hint.SupplierProductCode, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateMapping(hint.MappedMaterialCode);
        }
        else
        {
            await dbContext.SupplierMaterialHints.AddAsync(hint, cancellationToken);
        }
    }

    public async Task<List<SupplierMaterialHintResult>> GetSuggestionsAsync(
        string? gtin, 
        string? ncm, 
        string? description, 
        string? supplierFiscalId, 
        string? supplierProductCode, 
        CancellationToken cancellationToken)
    {
        var results = new List<SupplierMaterialHintResult>();
        var addedCodes = new HashSet<string>();

        if (!string.IsNullOrWhiteSpace(supplierFiscalId) && !string.IsNullOrWhiteSpace(supplierProductCode))
        {
            var fiscalId = FiscalId.From(supplierFiscalId);
            var hint = await dbContext.SupplierMaterialHints
                .FirstOrDefaultAsync(x => x.SupplierFiscalId == fiscalId && x.SupplierProductCode == supplierProductCode, cancellationToken);

            if (hint is not null)
            {
                var material = await dbContext.Materials.FirstOrDefaultAsync(x => x.MaterialCode == hint.MappedMaterialCode, cancellationToken);
                if (material is not null && addedCodes.Add(material.MaterialCode.Value))
                {
                    results.Add(new SupplierMaterialHintResult(material, "High", "Supplier Mapping"));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(gtin))
        {
            var normalizedGtin = gtin.Trim();
            var materials = await dbContext.Materials
                .Where(x => x.Gtin == normalizedGtin)
                .ToListAsync(cancellationToken);

            foreach (var material in materials)
            {
                if (addedCodes.Add(material.MaterialCode.Value))
                {
                    results.Add(new SupplierMaterialHintResult(material, "High", "GTIN Match"));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(ncm) || !string.IsNullOrWhiteSpace(description))
        {
            var query = dbContext.Materials.AsQueryable();
            var hasCondition = false;
            var rank = "Low";

            if (!string.IsNullOrWhiteSpace(ncm))
            {
                var normalizedNcm = ncm.Trim();
                query = query.Where(x => x.Ncm == normalizedNcm);
                hasCondition = true;
                rank = "Medium";
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                var words = description.Split(new[] { ' ', ',', '.', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 0)
                {
                    var word = words[0];
                    var normalizedWord = $"%{word.Trim().ToLowerInvariant()}%";
                    query = query.Where(x => EF.Functions.ILike(x.Description, normalizedWord) || EF.Functions.ILike(x.OfficialName, normalizedWord));
                    hasCondition = true;
                }
            }

            if (hasCondition)
            {
                var materials = await query.Take(10).ToListAsync(cancellationToken);
                foreach (var material in materials)
                {
                    if (addedCodes.Add(material.MaterialCode.Value))
                    {
                        results.Add(new SupplierMaterialHintResult(material, rank, "NCM/Description Match"));
                    }
                }
            }
        }

        return results;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
