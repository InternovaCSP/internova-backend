namespace Internova.Core.Interfaces;

/// <summary>
/// Abstraction for uploading binary files to cloud blob storage.
/// Keeps Internova.Core free of any ASP.NET or Azure SDK references.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Validates and uploads a resume PDF for <paramref name="studentId"/>.
    /// </summary>
    /// <param name="stream">The readable stream of the file content.</param>
    /// <param name="fileName">Original filename (used to check .pdf extension).</param>
    /// <param name="contentType">MIME content type declared by the client.</param>
    /// <param name="fileSizeBytes">Length of the stream in bytes (used for size validation).</param>
    /// <param name="studentId">The user ID of the student (used to name the blob).</param>
    /// <returns>The absolute URL of the stored blob.</returns>
    /// <exception cref="ArgumentException">Thrown if file type or size is invalid.</exception>
    Task<string> UploadResumeAsync(Stream stream, string fileName, string contentType, long fileSizeBytes, int studentId);
}
