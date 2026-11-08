## Purpose

Admin endpoints let a developer inspect and adjust game content on a local machine. They share the game API process but live in their own URL namespace, so they can be switched off and kept out of the game's API contract.

## ADDED Requirements

### Requirement: Admin routes live under the admin namespace

Every admin endpoint SHALL be served under the `/admin/` path prefix, and no game endpoint SHALL be served under `/admin/`. Game endpoints SHALL keep their existing `/api/` paths.

#### Scenario: Admin and game routes are separated by URL

- **WHEN** the API starts with admin enabled
- **THEN** every route registered by the admin endpoint group starts with `/admin/`
- **AND** every existing game route still starts with `/api/` and responds as before

### Requirement: Admin is enabled only by configuration

The API SHALL register admin routes only when the setting `Admin:Enabled` is `true`. The default in `appsettings.json` SHALL be `false`, and `appsettings.Development.json` SHALL set it to `true`.

#### Scenario: Admin disabled

- **WHEN** the API runs with `Admin:Enabled` set to `false`
- **THEN** a request to any `/admin/` path returns 404 Not Found

#### Scenario: Admin enabled

- **WHEN** the API runs with `Admin:Enabled` set to `true`
- **THEN** `GET /admin/content/items` returns 200 OK

### Requirement: Admin has its own OpenAPI document

The API SHALL publish admin endpoints only in an OpenAPI document named `admin`. The game document `v1` SHALL NOT contain any `/admin/` path.

#### Scenario: Game API client is unaffected

- **WHEN** a developer runs `pnpm generate:api` in `frontend` against `/openapi/v1.json`
- **THEN** the generated schema contains no path starting with `/admin/`

#### Scenario: Admin document lists admin paths

- **WHEN** admin is enabled and a client requests `/openapi/admin.json`
- **THEN** the document contains the admin content, draft, and report paths and no `/api/` path
