## MODIFIED Requirements

### Requirement: Admin has its own OpenAPI document

The API SHALL publish admin endpoints only in an OpenAPI document named `admin`. The game document `v1` SHALL NOT contain any `/admin/` path.

#### Scenario: Game API client is unaffected

- **WHEN** a developer runs `pnpm generate:api` in `frontend`, which reads the `v1` document file under `backend/openapi/`
- **THEN** the generated schema contains no path starting with `/admin/`

#### Scenario: Admin document lists admin paths

- **WHEN** admin is enabled and a client requests `/openapi/admin.json`
- **THEN** the document contains the admin content, draft, and report paths and no `/api/` path
