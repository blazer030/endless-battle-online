## Purpose

The admin console is a separate web app for developers to browse and edit game content drafts, check balance reports, and publish. It is independent of the game frontend and its rules.

## ADDED Requirements

### Requirement: Content pages

The admin console SHALL provide a list page, an edit form, and a create form for each of the five editable content kinds, using the admin API. Saving a form SHALL write to the draft through the admin API.

#### Scenario: Edit a record

- **WHEN** the developer changes a monster's level in its edit form and saves
- **THEN** the admin API receives the update for that monster and the list shows the new level

### Requirement: Draft status and publishing

The admin console SHALL show on every page whether a draft exists and whether it is valid, with the loader's error message when invalid. Publish SHALL be available only when a draft exists and is valid. The console SHALL show the API's reason when publish is refused.

#### Scenario: Invalid draft blocks publish

- **WHEN** the draft is invalid
- **THEN** the console shows the error message and the publish action is disabled

#### Scenario: Publish refused because official content changed

- **WHEN** the developer publishes and the API responds 409 naming a changed official file
- **THEN** the console shows that file name and the draft remains

### Requirement: Report comparison

The admin console SHALL show one report table for the selected skill-level option. When a valid draft exists the table SHALL show the draft values, and every cell whose draft value differs from the official value SHALL be marked and show the official value followed by the draft value. When no draft exists the table SHALL show the official report.

#### Scenario: Compare after an edit

- **WHEN** a valid draft exists and the developer opens the report page
- **THEN** every cell whose draft value differs from the official value is marked and shows both values, and unchanged cells show only their value

##### Example: one changed cell

- **GIVEN** the official report gives warrior a wear level of 8 for one shop level and the draft report gives 10, and all other cells are equal
- **WHEN** the developer opens the report page
- **THEN** only that cell is marked and it reads `8 → 10`

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
