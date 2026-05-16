using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace RailFactory.Iam.Api.Infrastructure;

// Persists Data Protection XML keys to Redis via IDistributedCache.
// This prevents cookie invalidation when the IAM process restarts,
// because Redis is backed by a persistent volume in the Aspire setup.
internal sealed class DistributedCacheXmlRepository(IDistributedCache cache) : IXmlRepository
{
    private const string CacheKey = "iam:dataprotection:keys";
    private static readonly DistributedCacheEntryOptions KeyExpiry = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(90)
    };

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var data = cache.GetString(CacheKey);
        if (string.IsNullOrWhiteSpace(data))
            return [];

        try
        {
            return XDocument.Parse(data).Root?.Elements().ToArray() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        var existing = GetAllElements().ToList();
        existing.Add(element);
        var doc = new XDocument(new XElement("keys", existing));
        cache.SetString(CacheKey, doc.ToString(), KeyExpiry);
    }
}
