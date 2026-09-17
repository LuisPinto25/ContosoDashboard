<!--
Sync Impact Report
- Version change: template placeholder → 1.0.0
- Modified principles: n/a (replaced template placeholders with project-specific governance)
- Added sections: Core Principles, Additional Constraints, Development Workflow, Governance
- Removed sections: none
- Templates requiring updates: ✅ .specify/memory/constitution.md; ✅ reviewed and aligned: .specify/templates/plan-template.md, .specify/templates/spec-template.md, .specify/templates/tasks-template.md
- Follow-up TODOs: none
-->

# ContosoDashboard Constitution

## Core Principles

### I. Security and Trust by Default

Security controls are required before a feature is considered complete. All protected pages, services, and actions MUST enforce authentication and authorization, and no user may access another user's data, project, or task unless a valid permission path is explicitly granted. The project intentionally includes mock authentication for training, but any production deployment MUST replace it with a secure identity provider and proper secret management.

### II. User-Scoped Access and Data Integrity

Every action MUST be evaluated against the current user, role, and project membership before data is returned or modified. Data isolation is mandatory: each user must only see the projects, tasks, and notifications they are authorized to access, and service methods MUST reject unauthorized requests instead of relying on UI checks alone. Referential integrity and permission checks are expected for every create, read, update, and delete path.

### III. Offline-First Learning with Clear Abstraction Boundaries

This application MUST remain runnable in an offline training environment without external cloud dependencies. Infrastructure concerns such as data storage, authentication, and file handling MUST be abstracted behind interfaces and resolved through dependency injection so business logic remains portable and understandable. Local-first implementations are acceptable for education, but their limitations MUST be documented clearly.

### IV. Testable, Small, and Verifiable Changes

Features MUST be implemented in small, independently testable increments with evidence from real behavior, not assumptions. Changes that affect authentication, authorization, task flow, or project membership MUST be validated against the relevant user journey in the running application. When a requirement is ambiguous, the team MUST clarify it before implementation rather than shipping implicit behavior.

### V. Simplicity, Clarity, and Maintainability

The codebase MUST favor clear responsibilities, shallow dependencies, and descriptive naming over hidden coupling or clever abstractions. UI logic, business services, and persistence concerns should remain separable so the project remains easy to teach, debug, and extend. No layer may add complexity without a specific need, and any design decision with security or workflow impact MUST be documented.

## Additional Constraints

- This repository is a training-focused ASP.NET Core and Blazor Server application using SQLite and Entity Framework Core.
- The application MUST remain usable without cloud infrastructure and SHOULD prefer local, offline-friendly patterns during development and demos.
- Mock authentication and sample data are intentionally limited to training scenarios and MUST not be treated as production-grade security controls.
- All project features MUST preserve the separation between user experience, domain services, and data access.
- Security-sensitive changes MUST be reviewed with explicit attention to IDOR prevention, authorization checks, and user isolation.

## Development Workflow

- Features MUST be driven by explicit user needs and acceptance criteria before implementation begins.
- Work MUST be broken into small, demonstrable increments that can be validated independently.
- Before merge, the affected authentication, project, and task flows MUST be checked for permission correctness and user isolation.
- Documentation updates are required whenever architecture, security assumptions, or user workflows materially change.
- Team review MUST verify that the implementation still aligns with the project's offline-first training goals and maintainability standards.

## Governance

This Constitution supersedes informal project practices and defines the non-negotiable operating rules for ContosoDashboard. Any amendment requires a documented rationale, a version bump, and a review against the security, maintainability, and offline-first principles above. All changes that affect authentication, authorization, project scope, or data access MUST include validation of the affected user flow before completion.

- Amendments MUST be recorded in the constitution and must explain the problem being solved, the reason for the change, and any migration or compatibility impacts.
- Versioning MUST follow semantic versioning: MAJOR for breaking governance or principle changes, MINOR for meaningful additions or expansions, and PATCH for clarifications or non-semantic wording fixes.
- Pull requests and review artifacts MUST confirm that the change preserves user isolation, data integrity, and the training-oriented architecture constraints.
- Non-trivial changes require review by at least one person who can verify the change against this Constitution.
- Breaking changes that affect existing workflows MUST include a migration plan or explicit deprecation guidance.

**Version**: 1.0.0 | **Ratified**: 2026-09-16 | **Last Amended**: 2026-09-16
