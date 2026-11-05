# content-draft Specification

## Purpose

Content drafts let a developer edit or add game content records without touching the live content files. Changes stay in a draft until the developer confirms them with a balance report and publishes them back.

## Requirements

### Requirement: Editable content kinds

The admin SHALL support reading, updating, and creating records of exactly five content kinds: items (`items.json`), monsters (`monsters.json`), jobs (`jobs.json`), skills (`skills.json`), and world areas and places (`world.json`). Records SHALL be identified by their `id` field. Deleting records SHALL NOT be supported.

#### Scenario: List records of a kind

- **WHEN** the admin requests `GET /admin/content/monsters`
- **THEN** the response lists every monster record with all of its fields
- **AND** the records come from the draft when a draft exists, otherwise from the official content

#### Scenario: Unsupported kind

- **WHEN** the admin requests `GET /admin/content/recipes`
- **THEN** the response is 404 Not Found


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
### Requirement: Edits only change the draft

Updating or creating a record SHALL write only to the draft directory configured by `Admin:DraftDirectory`. The first write SHALL create the draft as a full copy of the official content directory and record a fingerprint of every official file. Official content files and the content loaded by the running game SHALL NOT change until publish.

#### Scenario: First edit creates the draft

- **WHEN** no draft exists and the admin updates item `iron-sword`
- **THEN** the draft directory contains all eleven content files with the updated `iron-sword`
- **AND** `backend/content/items.json` is byte-identical to before the request

##### Example: one field changed

- **GIVEN** official `iron-sword` has `"price": 200`
- **WHEN** the admin updates `iron-sword` with `"price": 250`
- **THEN** the draft `items.json` has `iron-sword` with `"price": 250` and every other item unchanged
- **AND** the game content the running API loaded at startup still has `iron-sword` with price 200

#### Scenario: Create a new record

- **WHEN** the admin creates a monster whose `id` does not exist
- **THEN** the draft `monsters.json` contains the new record after the existing ones

#### Scenario: Create with an existing id

- **WHEN** the admin creates an item whose `id` already exists
- **THEN** the response is 409 Conflict and the draft is unchanged


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
### Requirement: Every draft write is checked

After each write the system SHALL load the draft with the same loader the game uses at startup and return the result. A write that makes the draft invalid SHALL still be saved, so related records can be fixed in later writes.

#### Scenario: Write makes the draft invalid

- **WHEN** the admin updates a monster so its `placeId` points to a place that does not exist
- **THEN** the write is saved and the response reports the draft as invalid with the loader's error message

#### Scenario: Draft status

- **WHEN** the admin requests `GET /admin/draft`
- **THEN** the response states whether a draft exists, whether it is valid, and the current error message when invalid


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
### Requirement: Publish and discard

Publishing SHALL copy the draft's content files into the official content directory and then delete the draft. Publishing SHALL be refused when the draft is invalid or when any official file no longer matches the fingerprint recorded at draft creation. Discarding SHALL delete the draft without touching official content. Published content SHALL take effect in the game after the API restarts.

#### Scenario: Publish a valid draft

- **WHEN** the draft is valid, official content is unchanged since the draft was created, and the admin requests `POST /admin/draft/publish`
- **THEN** official content files equal the draft files, the draft directory is gone, and `GET /admin/draft` reports no draft

#### Scenario: Publish an invalid draft

- **WHEN** the draft is invalid and the admin requests publish
- **THEN** the response is 409 Conflict with the loader's error message and no official file changes

#### Scenario: Official content changed after the draft was created

- **WHEN** an official content file was modified after the draft was created and the admin requests publish
- **THEN** the response is 409 Conflict naming the changed file and no official file changes

#### Scenario: Discard the draft

- **WHEN** the admin requests `DELETE /admin/draft`
- **THEN** the draft directory is gone and official content files are unchanged


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
### Requirement: Stable file format

Content files written by the admin SHALL use one canonical format: two-space indentation with one record per line inside each top-level array. Writing a file whose records did not change SHALL produce bytes identical to the file it read.

#### Scenario: Unchanged records keep their bytes

- **WHEN** the admin updates one item in a draft whose files are already in canonical format
- **THEN** a line diff between the draft `items.json` before and after shows only that item's line

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