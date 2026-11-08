# admin-console Specification

## Purpose

The admin console is a separate web app for developers to browse and edit game content drafts, check balance reports, and publish. It is independent of the game frontend and its rules.

## Requirements

### Requirement: Content pages

The admin console SHALL provide a list page for each of the six editable content kinds. Editing and creating a record SHALL happen in a dialog opened from the list page without navigating away. Creating SHALL start either from a blank form or from a copy of an existing record with its id cleared. Saving SHALL write to the draft through the admin API; on success the dialog closes and the list and draft status refresh, and on failure the dialog stays open and shows the API's error. Every field of a kind's record SHALL be editable through a labelled form control, and the console SHALL NOT offer raw JSON editing.

#### Scenario: Edit a record

- **WHEN** the developer opens a monster's edit dialog from the monsters list, changes its level, and saves
- **THEN** the admin API receives the update for that monster, the dialog closes, and the list shows the new level without a page change

#### Scenario: Create from a copy

- **WHEN** the developer starts a new item from a copy of `item-2` 木刀, enters the id `item-9001`, and saves
- **THEN** the admin API receives a new item `item-9001` with `item-2`'s other field values

#### Scenario: Save rejected

- **WHEN** the developer saves a new record whose id already exists and the API responds 409
- **THEN** the dialog stays open and shows that the id already exists

---
### Requirement: Draft status and publishing

The admin console SHALL show on every page whether a draft exists and whether it is valid, with the loader's error message when invalid. Publish SHALL be available only when a draft exists and is valid. The console SHALL show the API's reason when publish is refused.

#### Scenario: Invalid draft blocks publish

- **WHEN** the draft is invalid
- **THEN** the console shows the error message and the publish action is disabled

#### Scenario: Publish refused because official content changed

- **WHEN** the developer publishes and the API responds 409 naming a changed official file
- **THEN** the console shows that file name and the draft remains


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
### Requirement: Report comparison

The admin console SHALL show one report table for the selected skill-level option. When a valid draft exists the table SHALL show the draft values, and every cell whose draft value differs from the official value SHALL be marked and show the official value followed by the draft value. When no draft exists the table SHALL show the official report.

#### Scenario: Compare after an edit

- **WHEN** a valid draft exists and the developer opens the report page
- **THEN** every cell whose draft value differs from the official value is marked and shows both values, and unchanged cells show only their value

##### Example: one changed cell

- **GIVEN** the official report gives warrior a wear level of 8 for one shop level and the draft report gives 10, and all other cells are equal
- **WHEN** the developer opens the report page
- **THEN** only that cell is marked and it reads `8 → 10`


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
### Requirement: Console layout

Every admin page SHALL have a header that stays at the top of the viewport and shows the current page's name together with the draft status and the publish and discard actions. The sidebar SHALL stay in place while the page content scrolls. Each sidebar entry (the six content kinds and the report page) SHALL have its own icon, and no two entries SHALL share an icon.

#### Scenario: Header on a content page

- **WHEN** the developer opens the monsters list
- **THEN** the header shows the page name for monsters and the draft status with the publish action

#### Scenario: Sidebar stays while scrolling

- **WHEN** the developer scrolls a long content list
- **THEN** the sidebar remains visible at the same position

#### Scenario: Distinct sidebar icons

- **WHEN** the developer opens any admin page
- **THEN** the seven sidebar entries each show a different icon

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
### Requirement: API responses become view objects

The admin console SHALL convert every content response from the admin API into view objects before any page uses it, and SHALL convert view objects back into the API shape when saving. A response missing a required field SHALL be reported as an error naming the field's path, and the page SHALL NOT show partial data. Fields the console does not recognize SHALL be ignored.

#### Scenario: Missing required field

- **WHEN** the monsters list response has a monster without `level`
- **THEN** the monsters page shows an error naming `monsters[index].level` and no monster rows

#### Scenario: Unknown field

- **WHEN** a monster in the response has an extra field the console does not know
- **THEN** the monsters page shows the monster normally

---
### Requirement: Related records are chosen, not typed

Fields that refer to other content SHALL be chosen from a list showing the related record's name, and SHALL save the related record's id. The choices SHALL come from the admin API so records added in the same draft are available. A monster's sub-places SHALL be chosen from the sub-place names of the monster's selected place.

#### Scenario: Choose a monster's place and sub-places

- **WHEN** the developer chooses place 中央平原 for a monster and ticks 綠風草地
- **THEN** the saved monster has the id of 中央平原 as `placeId` and that sub-place's index in `subPlaceIndexes`

##### Example: sub-places follow the place

- **GIVEN** place 1 中央平原 has sub-places 香茅綠地, 綠風草地, 白沙海灣, 眠獸洞穴 and a monster in place 1 has sub-places 綠風草地 and 白沙海灣 ticked
- **WHEN** the developer changes the monster's place to another place
- **THEN** the sub-place choices show the new place's sub-place names and none are ticked

#### Scenario: Choose drops and an exclusive mission

- **WHEN** the developer adds a drop by choosing the item 錢包 with drop rarity 90 and chooses the mission 打敗米列 as the exclusive mission
- **THEN** the saved monster has a drop with `item-204` and rarity 90, and `mission-2` as `exclusiveMissionId`

#### Scenario: Daily missions are not offered

- **WHEN** the developer opens a monster's exclusive mission choices
- **THEN** the choices list regular missions and no daily mission

#### Scenario: Choose the skills a monster can use

- **WHEN** the developer adds a skill to a monster by choosing 爆氣 with level 2 and activation rate 10
- **THEN** the saved monster has a skill with `skill-1`, level 2, and activation rate 10

#### Scenario: The server rejects a skill that no longer exists

- **WHEN** the developer saves a monster and the API responds 400 naming `skills[0].skillId`
- **THEN** the dialog stays open and shows that error

#### Scenario: Long choice lists can be searched

- **WHEN** the developer types 錢包 into a monster's drop item choices
- **THEN** the choices narrow to items whose name or id contains 錢包

#### Scenario: Choose a parent job

- **WHEN** the developer edits job 狂戰士
- **THEN** the parent job choices list every other job by name and not 狂戰士 itself

---
### Requirement: Structured fields are edited item by item

Grouped values SHALL be edited as individual labelled inputs rather than as text: job growth as a minimum and maximum for each of the seven stats, an item's stat bonuses and stat requirements as one input per stat, and a job's stat requirements as a list of rows that each choose a stat and its minimum. A skill's form SHALL set the maximum level first and then edit each level's learning cost, required proficiency, cooldown, conditions, costs, and effects; each condition, cost, and effect SHALL choose a kind and show only that kind's fields. An item's form SHALL show only the fields that belong to its category and consumable kind, and saving SHALL send only those fields. A monster marked as an NPC SHALL show only the fields an NPC uses.

#### Scenario: Edit job growth

- **WHEN** the developer sets warrior's 力量 growth to minimum 2 and maximum 4 and saves
- **THEN** the saved job's growth has strength `{ "minimum": 2, "maximum": 4 }` and the other stats unchanged

#### Scenario: Raising a skill's maximum level

- **WHEN** the developer raises a skill's maximum level from 10 to 11
- **THEN** the form has a new level 11 starting with level 10's values, and the saved skill has 11 levels

#### Scenario: Effect fields follow the kind

- **WHEN** the developer changes an effect's kind from `StatChange` to `Silence`
- **THEN** the form shows chance and rounds instead of target and stat
- **AND** the saved effect has only `kind`, `chance`, and `rounds`

#### Scenario: A skill with no conditions

- **WHEN** the developer removes every condition from a skill level and saves
- **THEN** the saved level has an empty `conditions` list

#### Scenario: Marking a monster as an NPC

- **WHEN** the developer marks a monster as an NPC and saves
- **THEN** the form shows name, place, sub-places, exclusive mission, encounter rate, and message
- **AND** the saved monster has zero combat stats and no drops or skills

#### Scenario: Item fields follow the category

- **WHEN** the developer changes a new item's category from weapon to key, chooses a place, and saves
- **THEN** the form shows the key's place and duration instead of bonus, requirements, and holes
- **AND** the saved item has a key section and no bonus or requirements

#### Scenario: Only a weapon has a weapon skill

- **WHEN** the developer edits a body armor
- **THEN** the form has no element or weapon skill fields

---
### Requirement: Lists can be searched and filtered

Each content list SHALL be paged and have a search box matching record id or name, case-insensitively and by partial text, and filters for that kind: items by category and shop level, monsters by place, jobs by tier, skills by job and slot type, places by area and restriction. Search and filters SHALL combine. Fields that refer to other content SHALL show the related record's name in the list.

#### Scenario: Search and filter together

- **WHEN** the developer filters monsters by place 中央平原 and searches for 史萊姆
- **THEN** the list shows only monsters in 中央平原 whose id or name contains 史萊姆

#### Scenario: Related names in the list

- **WHEN** the developer opens the monsters list
- **THEN** each monster's place column shows the place name rather than its id
