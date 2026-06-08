using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RailFactory.Frontend.Infrastructure;

/// <summary>
/// Implements image storage using a MinIO bucket with multi-tenant partitioning.
/// </summary>
public sealed class MinioImageStorage : IImageStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public MinioImageStorage(IConfiguration configuration)
    {
        var endpoint = configuration["Minio:Endpoint"] ?? "http://localhost:9000";
        var accessKey = configuration["Minio:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["Minio:SecretKey"] ?? "minioadmin";
        _bucketName = configuration["Minio:BucketName"] ?? "railfactory-images";

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(string tenantCode, string fileName, Stream stream, CancellationToken cancellationToken)
    {
        // 1. Ensure the bucket exists
        await EnsureBucketExistsAsync(cancellationToken);

        // 2. Put the object in MinIO under the tenant's partition
        var key = $"{tenantCode}/{fileName}";
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream
        };

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        putRequest.ContentType = extension switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);

        // 3. Return the virtual path corresponding to the feature area
        if (fileName.StartsWith("person_"))
        {
            return $"/api/hr/people/images/{tenantCode}/{fileName}";
        }

        return $"/api/inventory/materials/images/{tenantCode}/{fileName}";
    }

    /// <inheritdoc />
    public async Task<ImageFileResult?> GetAsync(string tenantCode, string fileName, CancellationToken cancellationToken)
    {
        var key = $"{tenantCode}/{fileName}";
        try
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, key, cancellationToken);
            return new ImageFileResult(response.ResponseStream, response.Headers.ContentType);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _bucketName }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // Bucket already exists, which is the expected case after initialization
        }
    }
}
