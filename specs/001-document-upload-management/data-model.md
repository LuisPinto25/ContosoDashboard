# Data Model: Document Upload and Management

## Overview

This feature extends the current ContosoDashboard domain with document-related storage, access, and activity records while preserving the project’s existing model conventions.

## Entities

### Document

Represents a file uploaded by a user and associated with either a project, a task, or personal work storage.

| Field            | Type     | Constraints             | Notes                                                                                   |
| ---------------- | -------- | ----------------------- | --------------------------------------------------------------------------------------- |
| DocumentId       | int      | PK, required            | Integer key consistent with existing user/project patterns in .NET 10 app               |
| Title            | string   | required, max 255       | Human-readable label                                                                    |
| Description      | string?  | max 2000                | Optional context                                                                        |
| Category         | string   | required                | One of project documents, team resources, personal files, reports, presentations, other |
| FileName         | string   | required, max 255       | Original file name or sanitized reference                                               |
| StoredFileName   | string   | required, max 255       | Generated unique server-side name                                                       |
| FilePath         | string   | required, max 500       | Relative file path or storage key                                                       |
| FileSizeBytes    | long     | required                | Uploaded file size                                                                      |
| MimeType         | string   | required, max 255       | MIME type such as `application/pdf`                                                     |
| UploadedByUserId | int      | required, FK to User    | Owner of the document                                                                   |
| ProjectId        | int?     | FK to Project           | Optional project association                                                            |
| TaskId           | int?     | FK to TaskItem          | Optional task association                                                               |
| UploadedAt       | DateTime | required                | Upload timestamp                                                                        |
| UpdatedAt        | DateTime | required                | Last metadata/file update                                                               |
| IsDeleted        | bool     | required, default false | Soft delete support if needed                                                           |

Relationships:

- Many documents belong to one uploader.
- Many documents may belong to one project.
- Many documents may belong to one task.
- Each document may have many share records.

### DocumentShare

Tracks explicit permission grants beyond project membership.

| Field           | Type     | Constraints              | Notes                                   |
| --------------- | -------- | ------------------------ | --------------------------------------- |
| DocumentShareId | int      | PK                       | Unique share record                     |
| DocumentId      | int      | required, FK to Document | Shared document                         |
| UserId          | int      | required, FK to User     | Recipient of access                     |
| SharedByUserId  | int      | required, FK to User     | User who granted access                 |
| SharedAt        | DateTime | required                 | Sharing timestamp                       |
| IsActive        | bool     | required                 | Allows revocation without deleting rows |

Relationships:

- One document has many share entries.
- One user receives many share grants.

### User

The existing user model remains the base identity and authorization model.

- Already includes email, role, department, and project membership.
- Document access checks should combine user role, project membership, ownership, and share records.

### Project

The existing project entity remains the primary project grouping for documents.

- Documents can be associated with a project by `ProjectId`.
- Project members should have view access based on project membership and project manager permission.

### TaskItem

The existing task model can optionally associate a document with a task.

- Task-level document attachments are a convenience relation and should inherit project context when applicable.

### Notification

Existing notification behavior is reused for document share and project activity events.

- A document share should trigger a notification to the recipient.
- A new project document may notify project members in the same project.

## Validation rules

- `Title` is required and should be non-empty after trimming.
- `Category` must match one of the allowed categories in the feature specification.
- `FileSizeBytes` must be less than or equal to 25 MB.
- `MimeType` must match an allowed supported type set.
- A file path must be generated server-side and must not use user-supplied file names directly.
- Access checks must reject unauthorized access before download or metadata view.

## State transitions

### Document lifecycle

- Draft/upload pending → validation → stored metadata + file saved → active document
- Active document → metadata update / file replacement
- Active document → shared with users
- Active document → deleted after confirmation

### Share lifecycle

- Created when a user shares a document.
- Active while share is valid.
- Revoked when share record is disabled or document is deleted.

## Notes

This design keeps the domain aligned with the current project’s patterns: integer keys, simple relational metadata, and role-driven access checks at the service layer.
