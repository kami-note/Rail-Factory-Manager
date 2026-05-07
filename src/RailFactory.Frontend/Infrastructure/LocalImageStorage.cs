namespace RailFactory.Frontend.Infrastructure;

public sealed class LocalImageStorage(IWebHostEnvironment environment) : IImageStorage
{
    private const string StorageFolderName = "storage/materials";

    public async Task<string> SaveAsync(string tenantCode, string fileName, Stream stream, CancellationToken cancellationToken)
    {
        var targetRoot = Path.Combine(environment.ContentRootPath, "App", StorageFolderName, tenantCode);
        Directory.CreateDirectory(targetRoot);
        
        var fullPath = Path.Combine(targetRoot, fileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        // Return the virtual path used by the serving endpoint
        return $"/api/inventory/materials/images/{tenantCode}/{fileName}";
    }

    public Task<ImageFileResult?> GetAsync(string tenantCode, string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(environment.ContentRootPath, "App", StorageFolderName, tenantCode, fileName);

        if (!File.Exists(filePath))
        {
            return Task.FromResult<ImageFileResult?>(null);
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        var stream = File.OpenRead(filePath);
        return Task.FromResult<ImageFileResult?>(new ImageFileResult(stream, contentType));
    }
}
