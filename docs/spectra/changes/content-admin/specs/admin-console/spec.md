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

The admin console SHALL show the draft report and the official report side by side for the same skill-level option, and mark cells whose values differ.

#### Scenario: Compare after an edit

- **WHEN** a valid draft exists and the developer opens the report page
- **THEN** both reports are shown and every cell whose draft value differs from the official value is marked
