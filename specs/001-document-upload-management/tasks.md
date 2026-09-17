# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature structure and the document-storage foundation needed by all stories.

- [ ] T001 Create the document feature directory and storage layout for local uploads in `ContosoDashboard/AppData/` and `specs/001-document-upload-management/`
- [ ] T002 [P] Define document storage configuration and service registration in `ContosoDashboard/Program.cs` and `ContosoDashboard/Services/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the data model, storage abstraction, and authorization infrastructure before any user story can be implemented.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T003 Create the document storage contract in `ContosoDashboard/Services/IFileStorageService.cs`
- [ ] T004 Create the local filesystem implementation in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [ ] T005 [P] Extend `ContosoDashboard/Data/ApplicationDbContext.cs` with `DbSet<Document>` and `DbSet<DocumentShare>` and add relational indexes
- [ ] T006 [P] Add the `Document` and `DocumentShare` models in `ContosoDashboard/Models/Document.cs` and `ContosoDashboard/Models/DocumentShare.cs`
- [ ] T007 Implement permission-aware document logic in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T008 Add document-specific authorization checks and validation rules in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T009 [P] Register the document services and storage implementation in `ContosoDashboard/Program.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Upload and categorize a work document (Priority: P1) 🎯 MVP

**Goal**: Allow employees to upload valid documents with required metadata and secure storage.

**Independent Test**: A user can select a valid file, provide required metadata, submit the form, and confirm the document appears in their allowed document list.

### Implementation for User Story 1

- [ ] T010 [P] [US1] Create the upload form and document management page shell in `ContosoDashboard/Pages/Documents.razor`
- [ ] T011 [US1] Implement upload validation, file-type checks, size enforcement, and unique generated file names in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T012 [US1] Save file metadata and storage path through the local storage service in `ContosoDashboard/Services/LocalFileStorageService.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [ ] T013 [US1] Add success/error feedback and upload progress handling in `ContosoDashboard/Pages/Documents.razor`
- [ ] T014 [US1] Add project association and category selection handling to the upload flow in `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently.

---

## Phase 4: User Story 2 - Browse and locate documents by project, category, or search (Priority: P1)

**Goal**: Make documents findable through browsing, filtering, and search while enforcing access rules.

**Independent Test**: A user can filter or search for documents and only sees documents they are authorized to access.

### Implementation for User Story 2

- [ ] T015 [P] [US2] Add the document list, sorting, and grid view in `ContosoDashboard/Pages/Documents.razor`
- [ ] T016 [US2] Implement document query filtering by category, project, and date in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T017 [US2] Add search by title, description, tags, uploader, and project in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T018 [US2] Enforce user authorization in document list and search results in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T019 [US2] Add the project document section and recent document summary in `ContosoDashboard/Pages/Projects.razor` and `ContosoDashboard/Pages/Index.razor`

**Checkpoint**: At this point, User Stories 1 and 2 should both work independently.

---

## Phase 5: User Story 3 - Share a document and manage who can access it (Priority: P2)

**Goal**: Allow document owners or project managers to share access and notify recipients.

**Independent Test**: A user shares a document with another authorized employee and the recipient sees it in the shared section with an in-app notification.

### Implementation for User Story 3

- [ ] T020 [P] [US3] Add share controls and recipient selection to `ContosoDashboard/Pages/Documents.razor`
- [ ] T021 [US3] Implement share creation and permission logic in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T022 [US3] Add recipient notification generation in `ContosoDashboard/Services/NotificationService.cs`
- [ ] T023 [US3] Add a shared-documents view and access checks in `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: At this point, the sharing flow should be independently usable and protected by authorization rules.

---

## Phase 6: User Story 4 - Maintain document lifecycle and audit visibility (Priority: P2)

**Goal**: Support metadata changes, replacement, deletion, and audit review in the document lifecycle.

**Independent Test**: An owner edits metadata, replaces a file, or confirms deletion and the action is reflected in the view with proper authorization enforcement.

### Implementation for User Story 4

- [ ] T024 [P] [US4] Add metadata edit and file replacement workflow in `ContosoDashboard/Pages/Documents.razor`
- [ ] T025 [US4] Implement edit, replace, and delete logic in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T026 [US4] Add deletion confirmation flow and active-view cleanup in `ContosoDashboard/Pages/Documents.razor`
- [ ] T027 [US4] Implement document activity logging for upload, download, share, and delete actions in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T028 [US4] Expose audit and admin-friendly reporting hooks in `ContosoDashboard/Pages/Index.razor` and `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: At this point, all document lifecycle operations should be independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, hardening, and integration cleanup across all stories.

- [ ] T029 [P] Review the document feature for security, authorization, and IDOR protection across `ContosoDashboard/Services/*` and `ContosoDashboard/Pages/*`
- [ ] T030 Review file path safety, extension validation, and storage cleanup logic in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [ ] T031 [P] Validate the feature against the scenarios in `specs/001-document-upload-management/quickstart.md`
- [ ] T032 Update the app documentation for the new document management flow in `README.md` and `StakeholderDocs/document-upload-and-management-feature.md`
- [ ] T033 [P] Run a final smoke test of upload, search, share, and deletion flows in the running Blazor app

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational completion
  - User story implementation can proceed in parallel when team capacity allows
  - Priority order should be US1, US2, US3, US4
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - no dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational - should be independently testable
- **User Story 3 (P2)**: Can start after US1 and Foundational - depends on upload and permission base
- **User Story 4 (P2)**: Can start after US1 and US3 - depends on document lifecycle and access model

### Parallel Opportunities

- `T002`, `T005`, `T006`, `T009` can run in parallel in the foundational setup phase
- `T010`, `T013`, `T015`, `T020`, `T024` can be developed in parallel within story implementation when each task targets different UI surfaces or service concerns
- Story-level work can occur in parallel across multiple developers after the foundational tasks complete

---

## Parallel Example: MVP Story 1

```bash
# Upload UI and metadata work
Task: "Create the upload form and document management page shell in ContosoDashboard/Pages/Documents.razor"
Task: "Add the local storage implementation in ContosoDashboard/Services/LocalFileStorageService.cs"

# Upload service work
Task: "Implement upload validation and unique file naming in ContosoDashboard/Services/DocumentService.cs"
Task: "Save metadata and storage path through the local storage service in ContosoDashboard/Services/DocumentService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Verify upload and validation behavior against the quickstart scenarios
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → shared storage and authorization model ready
2. Add User Story 1 → upload and metadata flow
3. Add User Story 2 → browse, filter, and search
4. Add User Story 3 → sharing and notifications
5. Add User Story 4 → lifecycle and audit support
6. Final polish and validation
