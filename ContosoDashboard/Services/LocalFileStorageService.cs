using Microsoft.AspNetCore.StaticFiles;

namespace ContosoDashboard.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
  private readonly string _storageRoot;
  private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

  public LocalFileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
  {
    var configuredRoot = configuration["DocumentStorage:RootPath"] ?? "AppData/uploads";
    _storageRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
    Directory.CreateDirectory(_storageRoot);
  }

  public async Task<FileStorageResult> UploadAsync(
      Stream content,
      string originalFileName,
      string contentType,
      CancellationToken cancellationToken = default)
  {
    var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
    var storedFileName = $"{Guid.NewGuid():N}{extension}";
    var destinationPath = GetSafePath(storedFileName);

    await using var destination = new FileStream(
        destinationPath,
        FileMode.CreateNew,
        FileAccess.Write,
        FileShare.None,
        bufferSize: 81920,
        useAsync: true);
    await content.CopyToAsync(destination, cancellationToken);

    var storedContentType = string.IsNullOrWhiteSpace(contentType)
        ? GetContentType(extension)
        : contentType;

    return new FileStorageResult(
        storedFileName,
        Path.Combine("uploads", storedFileName).Replace(Path.DirectorySeparatorChar, '/'),
        destination.Length,
        storedContentType);
  }

  public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default)
  {
    var path = GetSafePath(storedFileName);
    if (File.Exists(path))
    {
      File.Delete(path);
    }

    return Task.CompletedTask;
  }

  public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
  {
    var path = GetSafePath(storedFileName);
    if (!File.Exists(path))
    {
      return Task.FromResult<Stream?>(null);
    }

    Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
    return Task.FromResult<Stream?>(stream);
  }

  public string GetUrl(string storedFileName)
  {
    _ = GetSafePath(storedFileName);
    return $"/documents/file/{Uri.EscapeDataString(storedFileName)}";
  }

  private string GetSafePath(string storedFileName)
  {
    var safeName = Path.GetFileName(storedFileName);
    if (string.IsNullOrWhiteSpace(safeName) || !string.Equals(safeName, storedFileName, StringComparison.Ordinal))
    {
      throw new ArgumentException("The stored file name is invalid.", nameof(storedFileName));
    }

    return Path.Combine(_storageRoot, safeName);
  }

  private string GetContentType(string extension)
  {
    return _contentTypeProvider.TryGetContentType(extension, out var contentType)
        ? contentType
        : "application/octet-stream";
  }
}
