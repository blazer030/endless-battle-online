# admin-access Specification

## Purpose

Admin endpoints let a developer inspect and adjust game content on a local machine. They share the game API process but live in their own URL namespace, so they can be switched off and kept out of the game's API contract.

## Requirements

### Requirement: Admin routes live under the admin namespace

Every admin endpoint SHALL be served under the `/admin/` path prefix, and no game endpoint SHALL be served under `/admin/`. Game endpoints SHALL keep their existing `/api/` paths.

#### Scenario: Admin and game routes are separated by URL

- **WHEN** the API starts with admin enabled
- **THEN** every route registered by the admin endpoint group starts with `/admin/`
- **AND** every existing game route still starts with `/api/` and responds as before


<!-- @trace
source: content-admin
updated: 2026-11-05
code:
  - admin/src/api/draft.ts
  - admin/src/draft/DraftBar.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminEndpoints.cs
  - admin/src/api/report.ts
  - backend/EndlessBattle.Api/appsettings.json
  - backend/EndlessBattle.Application/DependencyInjection.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminOpenApiTests.cs
  - admin/src/App.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentUpdateTests.cs
  - admin/.prettierrc
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - admin/src/main.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminDraftEndpoints.cs
  - admin/src/content/ContentPages.tsx
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
  - admin/src/content/recordFields.ts
  - admin/src/content/resources.ts
  - admin/src/layout/AdminHeader.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminReportEndpoints.cs
  - backend/EndlessBattle.Infrastructure/DependencyInjection.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminAccessTests.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftCheckTests.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminContentEndpoints.cs
  - backend/EndlessBattle.Tests/Support/AdminContentDirectories.cs
  - backend/content/skills.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminReportTests.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/ContentFileFormatterTests.cs
  - docs/research/wog45/README.md
  - docs/research/wog45/export.py
  - backend/EndlessBattle.Application/Admin/IContentDraftStore.cs
  - admin/tsconfig.json
  - backend/EndlessBattle.Api/appsettings.Development.json
  - backend/EndlessBattle.Api/Program.cs
  - admin/vite.config.ts
  - CLAUDE.md
  - backend/content/monsters.json
  - admin/tsconfig.app.json
  - backend/EndlessBattle.Application/Admin/ContentKind.cs
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - admin/src/api/dataProvider.ts
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - admin/src/content/RecordForm.tsx
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/layout/AdminSider.test.tsx
  - admin/src/content/ContentPages.test.tsx
  - admin/src/content/recordFields.test.ts
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->

---
### Requirement: Admin is enabled only by configuration

The API SHALL register admin routes only when the setting `Admin:Enabled` is `true`. The default in `appsettings.json` SHALL be `false`, and `appsettings.Development.json` SHALL set it to `true`.

#### Scenario: Admin disabled

- **WHEN** the API runs with `Admin:Enabled` set to `false`
- **THEN** a request to any `/admin/` path returns 404 Not Found

#### Scenario: Admin enabled

- **WHEN** the API runs with `Admin:Enabled` set to `true`
- **THEN** `GET /admin/content/items` returns 200 OK


<!-- @trace
source: content-admin
updated: 2026-11-05
code:
  - admin/src/api/draft.ts
  - admin/src/draft/DraftBar.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminEndpoints.cs
  - admin/src/api/report.ts
  - backend/EndlessBattle.Api/appsettings.json
  - backend/EndlessBattle.Application/DependencyInjection.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminOpenApiTests.cs
  - admin/src/App.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentUpdateTests.cs
  - admin/.prettierrc
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - admin/src/main.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminDraftEndpoints.cs
  - admin/src/content/ContentPages.tsx
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
  - admin/src/content/recordFields.ts
  - admin/src/content/resources.ts
  - admin/src/layout/AdminHeader.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminReportEndpoints.cs
  - backend/EndlessBattle.Infrastructure/DependencyInjection.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminAccessTests.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftCheckTests.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminContentEndpoints.cs
  - backend/EndlessBattle.Tests/Support/AdminContentDirectories.cs
  - backend/content/skills.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminReportTests.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/ContentFileFormatterTests.cs
  - docs/research/wog45/README.md
  - docs/research/wog45/export.py
  - backend/EndlessBattle.Application/Admin/IContentDraftStore.cs
  - admin/tsconfig.json
  - backend/EndlessBattle.Api/appsettings.Development.json
  - backend/EndlessBattle.Api/Program.cs
  - admin/vite.config.ts
  - CLAUDE.md
  - backend/content/monsters.json
  - admin/tsconfig.app.json
  - backend/EndlessBattle.Application/Admin/ContentKind.cs
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - admin/src/api/dataProvider.ts
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - admin/src/content/RecordForm.tsx
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/layout/AdminSider.test.tsx
  - admin/src/content/ContentPages.test.tsx
  - admin/src/content/recordFields.test.ts
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->

---
### Requirement: Admin has its own OpenAPI document

The API SHALL publish admin endpoints only in an OpenAPI document named `admin`. The game document `v1` SHALL NOT contain any `/admin/` path.

#### Scenario: Game API client is unaffected

- **WHEN** a developer runs `pnpm generate:api` in `frontend` against `/openapi/v1.json`
- **THEN** the generated schema contains no path starting with `/admin/`

#### Scenario: Admin document lists admin paths

- **WHEN** admin is enabled and a client requests `/openapi/admin.json`
- **THEN** the document contains the admin content, draft, and report paths and no `/api/` path

<!-- @trace
source: content-admin
updated: 2026-11-05
code:
  - admin/src/api/draft.ts
  - admin/src/draft/DraftBar.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminEndpoints.cs
  - admin/src/api/report.ts
  - backend/EndlessBattle.Api/appsettings.json
  - backend/EndlessBattle.Application/DependencyInjection.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminOpenApiTests.cs
  - admin/src/App.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentUpdateTests.cs
  - admin/.prettierrc
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - admin/src/main.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminDraftEndpoints.cs
  - admin/src/content/ContentPages.tsx
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
  - admin/src/content/recordFields.ts
  - admin/src/content/resources.ts
  - admin/src/layout/AdminHeader.tsx
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminReportEndpoints.cs
  - backend/EndlessBattle.Infrastructure/DependencyInjection.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminAccessTests.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftCheckTests.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Api/Endpoints/Admin/AdminContentEndpoints.cs
  - backend/EndlessBattle.Tests/Support/AdminContentDirectories.cs
  - backend/content/skills.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminReportTests.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/ContentFileFormatterTests.cs
  - docs/research/wog45/README.md
  - docs/research/wog45/export.py
  - backend/EndlessBattle.Application/Admin/IContentDraftStore.cs
  - admin/tsconfig.json
  - backend/EndlessBattle.Api/appsettings.Development.json
  - backend/EndlessBattle.Api/Program.cs
  - admin/vite.config.ts
  - CLAUDE.md
  - backend/content/monsters.json
  - admin/tsconfig.app.json
  - backend/EndlessBattle.Application/Admin/ContentKind.cs
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - admin/src/api/dataProvider.ts
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - admin/src/content/RecordForm.tsx
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/layout/AdminSider.test.tsx
  - admin/src/content/ContentPages.test.tsx
  - admin/src/content/recordFields.test.ts
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->