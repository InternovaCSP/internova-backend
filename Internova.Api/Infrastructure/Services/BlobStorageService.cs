using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Internova.Api.Application.Interfaces;

namespace Internova.Api.Infrastructure.Services;

/// <summary>
/// Uploads validated resume PDFs and profile images to Azure Blob Storage.
/// </summary>
public class BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger) : IBlobStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    private readonly string _connectionString = configuration["AzureBlob:ConnectionString"]
        ?? throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");
    private readonly string _resumeContainer = configuration["AzureBlob:ContainerName"] ?? "resumes";

    public Task<string> UploadResumeAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        int studentId)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) || extension != ".pdf")
            throw new ArgumentException("Only PDF files are accepted. Please upload a .pdf file.");

        var blobName = $"{studentId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_resume.pdf";
        return UploadValidatedAsync(stream, fileSizeBytes, _resumeContainer, blobName, "application/pdf", PublicAccessType.None);
    }

    public Task<string> UploadImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        int userId)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var isImage = (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
                       contentType == "application/octet-stream") &&
                      ImageExtensions.Contains(extension);

        if (!isImage)
            throw new ArgumentException("Only JPG and PNG images are accepted.");

        var blobName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{extension}";
        return UploadValidatedAsync(stream, fileSizeBytes, "profiles", blobName, contentType, PublicAccessType.Blob);
    }

    private async Task<string> UploadValidatedAsync(
        Stream stream,
        long fileSizeBytes,
        string containerName,
        string blobName,
        string contentType,
        PublicAccessType accessType)
    {
        if (stream is null || fileSizeBytes == 0)
            throw new ArgumentException("File is required.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException($"File must not exceed 5 MB. Received {fileSizeBytes / 1024.0 / 1024.0:F2} MB.");

        var container = new BlobServiceClient(_connectionString).GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(accessType);

        var blob = container.GetBlobClient(blobName);
        logger.LogInformation("Uploading blob '{BlobName}' to container '{Container}'.", blobName, containerName);

        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return blob.Uri.ToString();
    }
}
