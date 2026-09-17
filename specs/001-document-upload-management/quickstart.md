# Quickstart: Document Upload and Management Validation

## Prerequisites

- .NET 10 SDK
- SQLite support through EF Core
- Local project checkout of ContosoDashboard
- A browser to open the app locally

## Setup

1. Open a terminal in the project root.
2. Run the app:
   ```bash
   cd ContosoDashboard
   dotnet run
   ```
3. Navigate to the local Blazor app URL, typically `http://localhost:5000` or the URL shown in the console.
4. Log in using one of the seeded users from the dashboard authentication flow.

## Validation scenarios

### 1. Upload a valid document

- Navigate to the document management page or the project task area where document upload is available.
- Select a valid PDF or image file under 25 MB.
- Enter a title, category, optional description, and optional project association.
- Submit the upload.
- Expected result: the upload succeeds and the document appears in the current user’s document list.

### 2. Reject an invalid upload

- Try uploading an unsupported file type or a file larger than 25 MB.
- Expected result: the system shows a clear validation message and does not create a document record.

### 3. Project visibility and authorization

- Upload a project document while logged in as a project member or manager.
- Log out and log in as another user who is not in the project.
- Expected result: the second user cannot see or access the file through project or search views.

### 4. Search and filter documents

- Search by title, tag, description, or uploader name.
- Filter by category or project.
- Expected result: only documents the current user can access appear in the results.

### 5. Share and notify

- Share a document with another user.
- Expected result: the recipient receives an in-app notification and sees the document in their shared documents area.

### 6. Update or delete metadata

- Edit the title or category for a document you own.
- Replace the document with a new file version.
- Confirm deletion for a document you own or are authorized to remove.
- Expected result: the record updates successfully and the document is removed from active views after confirmation.

## Success indicators

- File upload completes without orphaned database records.
- Project members see only documents they are authorized to access.
- Search results reflect document ownership and share rules.
- Notification and audit actions are visible to the user and system admin.

## Exit criteria

The feature is ready to move to implementation when the upload, authorization, search, and sharing flows successfully work in the running app without requiring cloud services.
