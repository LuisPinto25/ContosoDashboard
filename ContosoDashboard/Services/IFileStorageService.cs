namespace ContosoDashboard.Services;

public interface IFileStorageService
{
  Task<FileStorageResult> UploadAsync(
      Stream content,
      string originalFileName,
      string contentType,
      CancellationToken cancellationToken = default);

  Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

  Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);

  string GetUrl(string storedFileName);
}

public sealed record FileStorageResult(
    string StoredFileName,
    string RelativePath,
    long FileSizeBytes,
    string ContentType);
