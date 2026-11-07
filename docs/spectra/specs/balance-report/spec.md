# balance-report Specification

## Purpose

The balance report turns a set of game content into numbers a designer can compare: when each job can wear each shop tier, and how a reference character fares in each place. It runs the game's own domain simulation, so formula changes show up without editing the report.

## Requirements

### Requirement: Report source

The system SHALL generate a balance report from either the official content or the draft, chosen per request. A report request for the draft when no draft exists SHALL return 404 Not Found. A report request for an invalid draft SHALL return 409 Conflict with the loader's error message.

#### Scenario: Report the draft

- **WHEN** a valid draft exists and the admin requests `POST /admin/reports` with source `draft`
- **THEN** the report is computed from the draft content and the official content is not read for it

#### Scenario: Same input, same report

- **WHEN** the admin requests the same report twice for unchanged content
- **THEN** both responses are identical


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
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
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
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->

---
### Requirement: Wear levels

The report SHALL list, for every starting-tier job and every requirement stat, the lowest character level at which the reference character's base stats meet the lowest requirement of each shop level. Levels above 100 SHALL be reported as unreachable.

#### Scenario: Requirement beyond level 100

- **WHEN** a job's expected growth never reaches a shop level's requirement by level 100
- **THEN** that cell is reported as unreachable instead of a level


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
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
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
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->

---
### Requirement: Adventure results per place

For every place that has monsters and every starting-tier job, the report SHALL simulate an adventure with the domain adventure simulator, using a fixed random seed and fixed duration, and report fights, win rate, losses, timeouts, experience per hour, gross and net money per hour, levels gained, and hours of net income needed for the next shop tier's full equipment set.

#### Scenario: Reference character for a place

- **WHEN** the report simulates place P whose recommended level range starts at level L
- **THEN** the reference character is level L, has expected base stats for that level, and wears the highest shop-level equipment it meets the requirements for in every slot


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
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
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
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->

---
### Requirement: Skills in the report

Each adventure result SHALL be reported twice: without skills, and with the job's skills equipped. With skills, the reference character SHALL equip the job's active skills in active slots and passive skills in passive slots, in content order, up to the slot limits of the domain skill loadout. The skill level SHALL be chosen per request as either level 1 or each skill's maximum level, defaulting to maximum.

#### Scenario: Skill change shows in the report

- **WHEN** the admin raises a warrior skill's chance in the draft and reports the draft with skills
- **THEN** the warrior's with-skills results differ from the official report while the without-skills results are the same

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
  - admin/tsconfig.node.json
  - backend/EndlessBattle.Tests/Api/Admin/AdminDraftPublishTests.cs
  - admin/src/report/compare.ts
  - backend/EndlessBattle.Infrastructure/Content/FileContentDraftStore.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentCreateTests.cs
  - admin/eslint.config.js
  - backend/EndlessBattle.Application/Balance/BalanceReport.cs
  - admin/.prettierignore
  - admin/index.html
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
  - backend/EndlessBattle.Application/Admin/DraftStatus.cs
  - backend/EndlessBattle.Application/Admin/ContentReportService.cs
  - backend/EndlessBattle.Tests/Support/HostConfigurationSetup.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Application/Admin/PublishResult.cs
  - backend/content/world.json
  - scripts/check.sh
  - backend/EndlessBattle.Infrastructure/Content/ContentFileFormatter.cs
  - backend/content/jobs.json
  - .spectra.yaml
  - admin/src/report/ReportPage.tsx
  - backend/EndlessBattle.Application/Admin/ContentAdminService.cs
  - admin/src/api/schema.ts
  - admin/package.json
tests:
  - admin/src/report/compare.test.ts
  - admin/src/App.test.tsx
  - admin/src/draft/DraftBar.test.tsx
  - admin/src/test/fetchMock.ts
  - admin/src/layout/AdminHeader.test.tsx
  - admin/src/test/setup.ts
  - admin/src/report/ReportPage.test.tsx
-->