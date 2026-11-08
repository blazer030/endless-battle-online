## Purpose

API documents are the contract between the backend and its two web clients. Generating them as files at build time lets both clients regenerate their API types without a running API, and lets the check script catch types that no longer match the backend.

## ADDED Requirements

### Requirement: OpenAPI documents are written at build time

Building `EndlessBattle.Api` SHALL write the `v1` and `admin` OpenAPI documents as files under `backend/openapi/`. The `admin` document SHALL include the admin routes even though `Admin:Enabled` defaults to `false`. The files SHALL be committed to the repository.

#### Scenario: Build writes both documents

- **WHEN** a developer runs `dotnet build` in `backend`
- **THEN** `backend/openapi/` contains the `v1` document with no `/admin/` path and the `admin` document with only `/admin/` paths

### Requirement: Clients generate types from the document files

`pnpm generate:api` in `frontend` SHALL generate `frontend/src/api/schema.ts` from the `v1` document file, and `pnpm generate:api` in `admin` SHALL generate `admin/src/api/schema.ts` from the `admin` document file. Neither command SHALL require the API to be running.

#### Scenario: Generate without a running API

- **WHEN** no API process is running and a developer runs `pnpm generate:api` in `admin`
- **THEN** `admin/src/api/schema.ts` is regenerated from `backend/openapi/` and the command succeeds

### Requirement: Stale API types fail the check

`scripts/check.sh` SHALL regenerate the document files and both `schema.ts` files and SHALL exit with a non-zero code and name the file when regenerating changes any of them or when any of them differs from what is staged in git.

#### Scenario: Backend changed without regenerating types

- **WHEN** an admin record gains a field and the developer runs `./scripts/check.sh` without regenerating `admin/src/api/schema.ts`
- **THEN** the check exits with a non-zero code and names the out-of-date file
