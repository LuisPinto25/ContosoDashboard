using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
  IReadOnlyList<string> Categories { get; }
  Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default);
  Task<List<Document>> GetUserDocumentsAsync(int userId, CancellationToken cancellationToken = default);
  Task<List<Document>> SearchAsync(DocumentSearchOptions options, int userId, CancellationToken cancellationToken = default);
  Task<List<Document>> GetProjectDocumentsAsync(int projectId, int userId, CancellationToken cancellationToken = default);
  Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5, CancellationToken cancellationToken = default);
  Task<List<Document>> GetSharedDocumentsAsync(int userId, CancellationToken cancellationToken = default);
  Task<DocumentActionResult> ShareAsync(int documentId, int recipientUserId, int requestingUserId, CancellationToken cancellationToken = default);
  Task<DocumentActionResult> UpdateMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default);
  Task<DocumentActionResult> ReplaceFileAsync(int documentId, DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default);
  Task<DocumentActionResult> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
  Task<DocumentFileResult?> OpenAuthorizedFileAsync(string storedFileName, int requestingUserId, CancellationToken cancellationToken = default);
  Task<DocumentAuditSummary?> GetAuditSummaryAsync(int requestingUserId, CancellationToken cancellationToken = default);
}

public sealed record DocumentUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Title,
    string? Description,
    string Category,
    int? ProjectId,
    string? Tags = null);

public sealed record DocumentSearchOptions(
    string? SearchTerm = null,
    string? Category = null,
    int? ProjectId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string SortBy = "UploadedAt",
    bool Descending = true);

public sealed record DocumentMetadataUpdate(string Title, string? Description, string Category, string? Tags);
public sealed record DocumentUploadResult(bool Succeeded, string? ErrorMessage, Document? Document)
{
  public static DocumentUploadResult Success(Document document) => new(true, null, document);
  public static DocumentUploadResult Failure(string message) => new(false, message, null);
}

public sealed record DocumentActionResult(bool Succeeded, string? ErrorMessage)
{
  public static DocumentActionResult Success() => new(true, null);
  public static DocumentActionResult Failure(string message) => new(false, message);
}

public sealed record DocumentFileResult(Stream Content, string ContentType, string DownloadName);
public sealed record DocumentAuditSummary(int Uploads, int Downloads, int Shares, int Deletes, List<DocumentTypeSummary> TopTypes, List<DocumentUserSummary> TopUploaders);
public sealed record DocumentTypeSummary(string Type, int Count);
public sealed record DocumentUserSummary(string UserName, int Count);

public sealed class DocumentService : IDocumentService
{
  public const long DefaultMaxFileSizeBytes = 25 * 1024 * 1024;
  public static readonly IReadOnlyList<string> AllowedCategories = new[]
  {
        "Project documents", "Team resources", "Personal files", "Reports", "Presentations", "Other"
    };

  private static readonly IReadOnlyDictionary<string, string[]> SupportedTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
  {
    [".pdf"] = new[] { "application/pdf" },
    [".png"] = new[] { "image/png" },
    [".jpg"] = new[] { "image/jpeg", "image/jpg" },
    [".jpeg"] = new[] { "image/jpeg", "image/jpg" },
    [".txt"] = new[] { "text/plain" },
    [".csv"] = new[] { "text/csv", "application/vnd.ms-excel" },
    [".doc"] = new[] { "application/msword" },
    [".docx"] = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
    [".xls"] = new[] { "application/vnd.ms-excel" },
    [".xlsx"] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
    [".ppt"] = new[] { "application/vnd.ms-powerpoint" },
    [".pptx"] = new[] { "application/vnd.openxmlformats-officedocument.presentationml.presentation" }
  };

  private readonly ApplicationDbContext _context;
  private readonly IFileStorageService _fileStorage;
  private readonly INotificationService _notificationService;
  private readonly long _maxFileSizeBytes;

  public IReadOnlyList<string> Categories => AllowedCategories;

  public DocumentService(ApplicationDbContext context, IFileStorageService fileStorage, INotificationService notificationService, IConfiguration configuration)
  {
    _context = context;
    _fileStorage = fileStorage;
    _notificationService = notificationService;
    _maxFileSizeBytes = configuration.GetValue<long?>("DocumentStorage:MaxFileSizeBytes") ?? DefaultMaxFileSizeBytes;
  }

  public async Task<DocumentUploadResult> UploadAsync(DocumentUploadRequest request, int userId, CancellationToken cancellationToken = default)
  {
    var validationError = await ValidateRequestAsync(request, userId, cancellationToken);
    if (validationError != null) return DocumentUploadResult.Failure(validationError);

    FileStorageResult? storedFile = null;
    try
    {
      storedFile = await _fileStorage.UploadAsync(request.Content, request.FileName, request.ContentType, cancellationToken);
      if (storedFile.FileSizeBytes > _maxFileSizeBytes)
      {
        await _fileStorage.DeleteAsync(storedFile.StoredFileName, cancellationToken);
        return DocumentUploadResult.Failure("The file exceeds the 25 MB maximum size.");
      }

      var now = DateTime.UtcNow;
      var document = new Document
      {
        Title = request.Title.Trim(),
        Description = CleanOptional(request.Description, 2000),
        Category = request.Category,
        Tags = CleanOptional(request.Tags, 1000),
        FileName = Path.GetFileName(request.FileName),
        StoredFileName = storedFile.StoredFileName,
        FilePath = storedFile.RelativePath,
        FileSizeBytes = storedFile.FileSizeBytes,
        MimeType = storedFile.ContentType,
        UploadedByUserId = userId,
        ProjectId = request.ProjectId,
        UploadedAt = now,
        UpdatedAt = now
      };
      _context.Documents.Add(document);
      await _context.SaveChangesAsync(cancellationToken);
      await LogActivityAsync(document.DocumentId, userId, "Upload", cancellationToken);
      if (document.ProjectId.HasValue)
      {
        var memberIds = await _context.ProjectMembers
            .Where(member => member.ProjectId == document.ProjectId && member.UserId != userId)
            .Select(member => member.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var memberId in memberIds)
        {
          await _notificationService.CreateNotificationAsync(new Notification
          {
            UserId = memberId,
            Title = "New project document",
            Message = $"A new document was added to project work: {document.Title}",
            Type = NotificationType.ProjectUpdate,
            Priority = NotificationPriority.Informational
          });
        }
      }
      return DocumentUploadResult.Success(document);
    }
    catch
    {
      if (storedFile != null) await _fileStorage.DeleteAsync(storedFile.StoredFileName, cancellationToken);
      throw;
    }
  }

  public Task<List<Document>> GetUserDocumentsAsync(int userId, CancellationToken cancellationToken = default) =>
      SearchInternalAsync(new DocumentSearchOptions(), userId, cancellationToken, ownOnly: true);

  public Task<List<Document>> SearchAsync(DocumentSearchOptions options, int userId, CancellationToken cancellationToken = default) =>
      SearchInternalAsync(options, userId, cancellationToken);

  public Task<List<Document>> GetProjectDocumentsAsync(int projectId, int userId, CancellationToken cancellationToken = default) =>
      SearchInternalAsync(new DocumentSearchOptions(ProjectId: projectId), userId, cancellationToken);

  public async Task<List<Document>> GetRecentDocumentsAsync(int userId, int count = 5, CancellationToken cancellationToken = default)
  {
    var documents = await SearchInternalAsync(new DocumentSearchOptions(), userId, cancellationToken);
    return documents.Take(Math.Max(1, count)).ToList();
  }

  public Task<List<Document>> GetSharedDocumentsAsync(int userId, CancellationToken cancellationToken = default) =>
      SearchInternalAsync(new DocumentSearchOptions(), userId, cancellationToken, sharedOnly: true);

  public async Task<DocumentActionResult> ShareAsync(int documentId, int recipientUserId, int requestingUserId, CancellationToken cancellationToken = default)
  {
    var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
    if (document == null || !await CanManageAsync(document, requestingUserId, cancellationToken)) return DocumentActionResult.Failure("You do not have permission to share this document.");
    if (!await _context.Users.AnyAsync(u => u.UserId == recipientUserId, cancellationToken)) return DocumentActionResult.Failure("The selected recipient was not found.");
    if (recipientUserId == requestingUserId) return DocumentActionResult.Failure("You already have access to your own document.");

    var share = await _context.DocumentShares.FirstOrDefaultAsync(s => s.DocumentId == documentId && s.UserId == recipientUserId, cancellationToken);
    if (share == null) _context.DocumentShares.Add(new DocumentShare { DocumentId = documentId, UserId = recipientUserId, SharedByUserId = requestingUserId });
    else { share.IsActive = true; share.SharedByUserId = requestingUserId; share.SharedAt = DateTime.UtcNow; }
    await _context.SaveChangesAsync(cancellationToken);
    await LogActivityAsync(documentId, requestingUserId, "Share", cancellationToken, $"Shared with user {recipientUserId}");
    await _notificationService.NotifyDocumentSharedAsync(recipientUserId, document.Title);
    return DocumentActionResult.Success();
  }

  public async Task<DocumentActionResult> UpdateMetadataAsync(int documentId, DocumentMetadataUpdate update, int requestingUserId, CancellationToken cancellationToken = default)
  {
    var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
    if (document == null || !await CanManageAsync(document, requestingUserId, cancellationToken)) return DocumentActionResult.Failure("You do not have permission to edit this document.");
    var error = ValidateMetadata(update.Title, update.Category);
    if (error != null) return DocumentActionResult.Failure(error);
    document.Title = update.Title.Trim();
    document.Description = CleanOptional(update.Description, 2000);
    document.Category = update.Category;
    document.Tags = CleanOptional(update.Tags, 1000);
    document.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync(cancellationToken);
    await LogActivityAsync(documentId, requestingUserId, "Update", cancellationToken);
    return DocumentActionResult.Success();
  }

  public async Task<DocumentActionResult> ReplaceFileAsync(int documentId, DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken = default)
  {
    var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
    if (document == null || !await CanManageAsync(document, requestingUserId, cancellationToken)) return DocumentActionResult.Failure("You do not have permission to replace this document.");
    var error = ValidateFile(request.FileName, request.ContentType, request.FileSizeBytes);
    if (error != null) return DocumentActionResult.Failure(error);
    var storedFile = await _fileStorage.UploadAsync(request.Content, request.FileName, request.ContentType, cancellationToken);
    if (storedFile.FileSizeBytes > _maxFileSizeBytes)
    {
      await _fileStorage.DeleteAsync(storedFile.StoredFileName, cancellationToken);
      return DocumentActionResult.Failure("The file exceeds the 25 MB maximum size.");
    }
    try
    {
      var oldFile = document.StoredFileName;
      document.FileName = Path.GetFileName(request.FileName);
      document.StoredFileName = storedFile.StoredFileName;
      document.FilePath = storedFile.RelativePath;
      document.FileSizeBytes = storedFile.FileSizeBytes;
      document.MimeType = storedFile.ContentType;
      document.UpdatedAt = DateTime.UtcNow;
      await _context.SaveChangesAsync(cancellationToken);
      await _fileStorage.DeleteAsync(oldFile, cancellationToken);
      await LogActivityAsync(documentId, requestingUserId, "Replace", cancellationToken);
      return DocumentActionResult.Success();
    }
    catch
    {
      await _fileStorage.DeleteAsync(storedFile.StoredFileName, cancellationToken);
      throw;
    }
  }

  public async Task<DocumentActionResult> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
  {
    var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted, cancellationToken);
    if (document == null || !await CanManageAsync(document, requestingUserId, cancellationToken)) return DocumentActionResult.Failure("You do not have permission to delete this document.");
    document.IsDeleted = true;
    document.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync(cancellationToken);
    await _fileStorage.DeleteAsync(document.StoredFileName, cancellationToken);
    await LogActivityAsync(documentId, requestingUserId, "Delete", cancellationToken);
    return DocumentActionResult.Success();
  }

  public async Task<DocumentFileResult?> OpenAuthorizedFileAsync(string storedFileName, int requestingUserId, CancellationToken cancellationToken = default)
  {
    var document = await _context.Documents.Include(d => d.Project).Include(d => d.Shares).FirstOrDefaultAsync(d => d.StoredFileName == storedFileName && !d.IsDeleted, cancellationToken);
    if (document == null || !await HasAccessAsync(document, requestingUserId, cancellationToken)) return null;
    var content = await _fileStorage.OpenReadAsync(document.StoredFileName, cancellationToken);
    if (content == null) return null;
    await LogActivityAsync(document.DocumentId, requestingUserId, "Download", cancellationToken);
    return new DocumentFileResult(content, document.MimeType, document.FileName);
  }

  public async Task<DocumentAuditSummary?> GetAuditSummaryAsync(int requestingUserId, CancellationToken cancellationToken = default)
  {
    if (!await IsAdministratorAsync(requestingUserId, cancellationToken)) return null;
    var activities = _context.DocumentActivities.AsNoTracking();
    var typeCounts = await _context.Documents.AsNoTracking().Where(d => !d.IsDeleted).GroupBy(d => d.MimeType).Select(g => new DocumentTypeSummary(g.Key, g.Count())).OrderByDescending(x => x.Count).Take(5).ToListAsync(cancellationToken);
    var uploaderCounts = await _context.Documents.AsNoTracking().Where(d => !d.IsDeleted).Include(d => d.UploadedByUser).GroupBy(d => d.UploadedByUser.DisplayName).Select(g => new DocumentUserSummary(g.Key, g.Count())).OrderByDescending(x => x.Count).Take(5).ToListAsync(cancellationToken);
    return new DocumentAuditSummary(await activities.CountAsync(a => a.Action == "Upload", cancellationToken), await activities.CountAsync(a => a.Action == "Download", cancellationToken), await activities.CountAsync(a => a.Action == "Share", cancellationToken), await activities.CountAsync(a => a.Action == "Delete", cancellationToken), typeCounts, uploaderCounts);
  }

  private async Task<List<Document>> SearchInternalAsync(DocumentSearchOptions options, int userId, CancellationToken cancellationToken, bool ownOnly = false, bool sharedOnly = false)
  {
    var isAdmin = await IsAdministratorAsync(userId, cancellationToken);
    var query = _context.Documents.AsNoTracking().Include(d => d.Project).Include(d => d.UploadedByUser).Where(d => !d.IsDeleted);
    if (!isAdmin) query = query.Where(d => d.UploadedByUserId == userId || (d.ProjectId.HasValue && (d.Project!.ProjectManagerId == userId || d.Project.ProjectMembers.Any(pm => pm.UserId == userId))) || d.Shares.Any(s => s.UserId == userId && s.IsActive));
    if (ownOnly) query = query.Where(d => d.UploadedByUserId == userId);
    if (sharedOnly) query = query.Where(d => d.Shares.Any(s => s.UserId == userId && s.IsActive));
    if (!string.IsNullOrWhiteSpace(options.SearchTerm))
    {
      var term = options.SearchTerm.Trim().ToLower();
      query = query.Where(d => d.Title.ToLower().Contains(term) || (d.Description != null && d.Description.ToLower().Contains(term)) || (d.Tags != null && d.Tags.ToLower().Contains(term)) || d.UploadedByUser.DisplayName.ToLower().Contains(term) || (d.Project != null && d.Project.Name.ToLower().Contains(term)));
    }
    if (!string.IsNullOrWhiteSpace(options.Category)) query = query.Where(d => d.Category == options.Category);
    if (options.ProjectId.HasValue) query = query.Where(d => d.ProjectId == options.ProjectId.Value);
    if (options.FromDate.HasValue) query = query.Where(d => d.UploadedAt >= options.FromDate.Value.Date);
    if (options.ToDate.HasValue) query = query.Where(d => d.UploadedAt < options.ToDate.Value.Date.AddDays(1));
    query = options.SortBy.ToLowerInvariant() switch
    {
      "title" => options.Descending ? query.OrderByDescending(d => d.Title) : query.OrderBy(d => d.Title),
      "category" => options.Descending ? query.OrderByDescending(d => d.Category) : query.OrderBy(d => d.Category),
      "size" => options.Descending ? query.OrderByDescending(d => d.FileSizeBytes) : query.OrderBy(d => d.FileSizeBytes),
      _ => options.Descending ? query.OrderByDescending(d => d.UploadedAt) : query.OrderBy(d => d.UploadedAt)
    };
    return await query.ToListAsync(cancellationToken);
  }

  private async Task<bool> HasAccessAsync(Document document, int userId, CancellationToken cancellationToken) =>
      await IsAdministratorAsync(userId, cancellationToken) || document.UploadedByUserId == userId || (document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)), cancellationToken)) || document.Shares.Any(s => s.UserId == userId && s.IsActive);

  private async Task<bool> CanManageAsync(Document document, int userId, CancellationToken cancellationToken) =>
      document.UploadedByUserId == userId || await IsAdministratorAsync(userId, cancellationToken) || (document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId, cancellationToken));

  private Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken) => _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator, cancellationToken);

  private async Task LogActivityAsync(int documentId, int actorUserId, string action, CancellationToken cancellationToken, string? details = null)
  {
    _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, ActorUserId = actorUserId, Action = action, Details = details });
    await _context.SaveChangesAsync(cancellationToken);
  }

  private async Task<string?> ValidateRequestAsync(DocumentUploadRequest request, int userId, CancellationToken cancellationToken)
  {
    if (userId <= 0 || !await _context.Users.AnyAsync(u => u.UserId == userId, cancellationToken)) return "Your authenticated user could not be found.";
    var metadataError = ValidateMetadata(request.Title, request.Category);
    if (metadataError != null) return metadataError;
    var fileError = ValidateFile(request.FileName, request.ContentType, request.FileSizeBytes);
    if (fileError != null) return fileError;
    if (request.ProjectId.HasValue && !await _context.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)), cancellationToken)) return "You do not have permission to associate a document with that project.";
    return null;
  }

  private string? ValidateMetadata(string title, string category) => string.IsNullOrWhiteSpace(title) || title.Trim().Length > 255 ? "Enter a document title with no more than 255 characters." : !Categories.Contains(category, StringComparer.Ordinal) ? "Select a valid document category." : null;

  private string? ValidateFile(string fileName, string contentType, long fileSizeBytes)
  {
    if (fileSizeBytes <= 0 || fileSizeBytes > _maxFileSizeBytes) return "The file must be larger than zero and no bigger than 25 MB.";
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    if (!SupportedTypes.TryGetValue(extension, out var contentTypes)) return "This file type is not supported. Upload a PDF, image, Office document, CSV, or text file.";
    return !string.IsNullOrWhiteSpace(contentType) && !contentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase) ? "The file type does not match its extension." : null;
  }

  private static string? CleanOptional(string? value, int maxLength)
  {
    if (string.IsNullOrWhiteSpace(value)) return null;
    var trimmed = value.Trim();
    return trimmed[..Math.Min(trimmed.Length, maxLength)];
  }
}
