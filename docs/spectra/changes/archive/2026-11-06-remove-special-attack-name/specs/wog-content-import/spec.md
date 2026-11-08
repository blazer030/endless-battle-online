## MODIFIED Requirements

### Requirement: Data the rules cannot use is listed

Original data that the current rules cannot use SHALL be left out of the content files and listed in `docs/research/wog45/gaps.md` with its original id, name and reason. This SHALL cover:

- unsupported fields;
- records the current content validation would reject;
- skills the effect catalogue cannot express;
- references removed because their target was left out.

A monster's special attack name SHALL be left out of the content files and SHALL NOT be listed per monster, because it was decided not to be used rather than waiting for a rule; `gaps.md` SHALL state this decision once in the monster mapping notes.

The official content SHALL load without errors after every import.

#### Scenario: An NPC with level 0 is listed, not imported

- **GIVEN** an original monster whose level is 0
- **WHEN** the import runs
- **THEN** that monster is absent from `monsters.json`, and `gaps.md` lists its id and name with the reason that level must be at least 1

#### Scenario: Unsupported monster fields are listed

- **GIVEN** the original monster 1 史萊姆 with the special attack 猛烈一擊 and the skill 爆氣 Lv2 at rate 10
- **WHEN** the import runs
- **THEN** `monster-1` is imported without those fields, and `gaps.md` lists 爆氣 Lv2 rate 10 under monster 1 and does not mention 猛烈一擊

#### Scenario: A monster with only a special attack is not listed

- **GIVEN** the original monster 13 白蛇龍 with the special attack 白龍巨浪 and no skills
- **WHEN** the import runs
- **THEN** `gaps.md` has no row for monster 13 in its list of fields that were not moved

#### Scenario: A reference to an excluded record is removed and listed

- **WHEN** a mission rewards an item that was excluded from the import
- **THEN** that reward is absent from the mission, and `gaps.md` records the removal and names the excluded item

#### Scenario: Official content loads

- **WHEN** `JsonGameContent.Load` reads `backend/content` after the import
- **THEN** loading succeeds without a `GameContentException`
