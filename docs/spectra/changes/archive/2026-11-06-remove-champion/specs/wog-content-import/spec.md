## MODIFIED Requirements

### Requirement: Custom content without an original counterpart is kept

Custom content for which the original data has nothing comparable SHALL be kept with its existing id, and every kept record SHALL appear in the "kept custom content" section of `gaps.md` with its reason. This SHALL cover `inn.json`, and the areas, recommended levels and starting place in `world.json`. Custom items, monsters and mercenaries SHALL be kept only when a current feature needs them and no original record fills the same role. All other custom content SHALL be removed. The official content SHALL NOT include a `champion.json`, and the game SHALL load without one.

#### Scenario: Inn stays unchanged

- **WHEN** the import runs
- **THEN** `inn.json` is byte-identical to before the import, and `gaps.md` lists it as kept custom content

#### Scenario: Champion content is gone

- **WHEN** the import runs and `JsonGameContent.Load` reads `backend/content`
- **THEN** no `champion.json` exists in `backend/content`, `gaps.md` does not mention `champion.json`, and loading succeeds without a `GameContentException`

#### Scenario: Custom content with an original counterpart is removed

- **GIVEN** the custom monster `wandering-puff` that is not on the kept list
- **WHEN** the import runs
- **THEN** `wandering-puff` is absent from `monsters.json`
