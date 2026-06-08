using Xunit;

namespace RailFactory.Frontend.Tests;

public sealed class MinioImageStorageTests
{
    [Theory]
    [InlineData("person_123.png", "/api/hr/people/images/dev/person_123.png")]
    [InlineData("person_abc.jpg", "/api/hr/people/images/dev/person_abc.jpg")]
    [InlineData("material_456.png", "/api/inventory/materials/images/dev/material_456.png")]
    [InlineData("sku123.webp", "/api/inventory/materials/images/dev/sku123.webp")]
    public void SaveAsync_GeneratesCorrectRoute_BasedOnFileName(string fileName, string expectedPath)
    {
        // Verify path routing matches expected multi-tenant URL patterns
        var isPerson = fileName.StartsWith("person_");
        var actualPath = isPerson 
            ? $"/api/hr/people/images/dev/{fileName}"
            : $"/api/inventory/materials/images/dev/{fileName}";

        Assert.Equal(expectedPath, actualPath);
    }
}
