namespace RailFactory.Frontend.Infrastructure;

/// <summary>
/// Defines the contract for material image storage, abstraction the physical location (Disk, S3, etc).
/// </summary>
public interface IImageStorage
{
    /// <summary>
    /// Persists an image file and returns the storage path/key.
    /// </summary>
    Task<string> SaveAsync(string tenantCode, string fileName, Stream stream, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the image file stream.
    /// </summary>
    Task<ImageFileResult?> GetAsync(string tenantCode, string fileName, CancellationToken cancellationToken);
}

public sealed record ImageFileResult(Stream Stream, string ContentType);
