using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Internova.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Internova.Infrastructure.Services;

/// <summary>
/// Uploads validated resume PDFs to Azure Blob Storage.
/// Container is kept private (no public read access).
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly string _connectionString;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        _connectionString = configuration["AzureBlob:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlob:ConnectionString is not configured.");
        _containerName = configuration["AzureBlob:ContainerName"] ?? "resumes";
    }

    /// <inheritdoc/>
    public async Task<string> UploadResumeAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        int studentId)
    {
        // ── Validate ────────────────────────────────────────────────────────────

        if (stream is null || fileSizeBytes == 0)
            throw new ArgumentException("Resume file is required.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException(
                $"Resume file must not exceed 5 MB. Received {fileSizeBytes / 1024.0 / 1024.0:F2} MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var isPdf = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                    && extension == ".pdf";

        if (!isPdf)
            throw new ArgumentException("Only PDF files are accepted. Please upload a .pdf file.");

        // ── Ensure container exists (private) ───────────────────────────────────

        var serviceClient = new BlobServiceClient(_connectionString);
        var containerClient = serviceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        // ── Build unique blob name ──────────────────────────────────────────────

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var blobName = $"{studentId}_{timestamp}_resume.pdf";
        var blobClient = containerClient.GetBlobClient(blobName);

        // ── Upload stream ───────────────────────────────────────────────────────

        _logger.LogInformation("Uploading resume blob '{BlobName}' for student {StudentId}.", blobName, studentId);

        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" }
        });

        _logger.LogInformation("Resume blob '{BlobName}' uploaded successfully.", blobName);

        return blobClient.Uri.ToString();
    }

    /// <inheritdoc/>
    public async Task<string> UploadImageAsync(
        Stream stream,
        string fileName,
        string contentType,
        long fileSizeBytes,
        int userId)
    {
        // ── Validate ────────────────────────────────────────────────────────────

        if (stream is null || fileSizeBytes == 0)
            throw new ArgumentException("Image file is required.");

        if (fileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException(
                $"Image file must not exceed 5 MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var isImage = (contentType.StartsWith("image/") || contentType == "application/octet-stream")
                      && allowedExtensions.Contains(extension);

        if (!isImage)
            throw new ArgumentException("Only JPG and PNG images are accepted.");

        // ── Ensure container exists (public read access for images) ──────────────

        var serviceClient = new BlobServiceClient(_connectionString);
        var containerClient = serviceClient.GetBlobContainerClient("profiles");
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        // ── Build unique blob name ──────────────────────────────────────────────

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var blobName = $"{userId}_{timestamp}{extension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        // ── Upload stream ───────────────────────────────────────────────────────

        _logger.LogInformation("Uploading profile image '{BlobName}' for user {UserId}.", blobName, userId);

        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        _logger.LogInformation("Profile image '{BlobName}' uploaded successfully.", blobName);

        return blobClient.Uri.ToString();
    }
}
