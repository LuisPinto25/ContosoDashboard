# Research: Document Upload and Management

## Decision

The feature will be implemented as a single-project Blazor Server enhancement inside the existing ContosoDashboard architecture, using .NET 10, SQLite for metadata persistence, and a local file-storage abstraction for uploaded binaries.

## Rationale

- The application already follows a layered pattern with `Models`, `Data`, `Services`, and `Pages`, so the document feature should integrate rather than introduce a separate service boundary.
- The training requirement explicitly calls for offline-only operation, which makes a local filesystem store the correct default.
- The project already uses role-based authorization and project membership checks; document access should reuse the same pattern, not add a separate security model.
- Because the app uses integer primary keys for core entities, `DocumentId` and related foreign key references should stay integer-based for consistency with `UserId` and `ProjectId`.

## Alternatives considered

1. Cloud-first Azure Blob storage from the start
   - Rejected because the feature must remain offline and self-contained for training and local demos.

2. Storing uploaded binary data directly in the database
   - Rejected because it complicates large-file handling, security, and future migration to cloud storage.

3. Separate document microservice or API project
   - Rejected because the project is intentionally small and single-application for the learning scenario.

## Key design decisions

### Storage architecture

- Use a repository-local directory such as `AppData/uploads` or `Data/Uploads` outside `wwwroot`.
- Generate a server-side unique file name before database persistence to avoid orphaned rows and path traversal issues.
- Expose file access through app-controlled service methods instead of direct static file serving from `wwwroot`.

### Access controls

- Document ownership is granted to the uploader.
- Project documents are visible to project members and project managers as defined by project membership.
- Shared documents are granted through a `DocumentShare` relation, with in-app notifications sent to recipients.
- Service methods must enforce permissions before returning or mutating data.

### Data model choices

- Category is stored as a text string to keep the model simple and compatible with current training examples.
- File MIME type is stored as a string field sized to 255 characters to accommodate Office document MIME values.
- Folder path and filename are stored as relative or application-local paths to support later cloud migration without changing business logic.

## Follow-up decisions

- Add a file storage interface named `IFileStorageService` with `UploadAsync`, `DeleteAsync`, `DownloadAsync`, and `GetUrlAsync` support.
- Add a document service for validation, authorization, metadata persistence, and access enforcement.
- Add UI pages and components consistent with the current Blazor patterns for projects, tasks, and notifications.
