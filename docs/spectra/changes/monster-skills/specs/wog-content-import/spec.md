## ADDED Requirements

### Requirement: Monster and weapon skills are imported

The import SHALL write each imported monster's original skills to `monster-skills.json` with the imported skill id, level and original rate, and SHALL write each imported weapon's original skill to its `weaponSkill`. The level SHALL follow the rows of the skill's name, so a skill imported with a single level SHALL use level 1. When the skill's `Chance` is not the default `min(100, 30 + 10 × level)`, the written rate SHALL be `max(1, round(rate × Chance / 100))`, and `gaps.md` SHALL record the conversion. When a monster lists the same skill twice, only the row with the higher level SHALL be written and `gaps.md` SHALL list the other row. A monster skill row with no rate SHALL NOT be written and SHALL be listed in `gaps.md`. 假死 SHALL be imported with its default `Chance` and a `Stun` effect with the description's chance and `chanceByStat` intelligence 5.

#### Scenario: A monster's skill is imported

- **GIVEN** the original monster 1 史萊姆 with the skill 爆氣 Lv2 at rate 10
- **WHEN** the import runs
- **THEN** `monster-skills.json` has the row `monster-1`, `skill-1`, level 2, rate 10, and `gaps.md` does not list that skill

#### Scenario: A series skill uses level 1

- **GIVEN** the original monster 88 with the skill 魔盾-水 Lv2 at rate 10, where 魔盾-水 is imported with one level
- **WHEN** the import runs
- **THEN** `monster-skills.json` has that skill at level 1 with rate 10

#### Scenario: A repeated skill keeps the higher level

- **GIVEN** the original monster 139 盜墓者 with 急影 Lv2 at rate 16 and 急影 Lv1 at rate 19
- **WHEN** the import runs
- **THEN** `monster-skills.json` has 急影 once for `monster-139`, at level 2 with rate 16, and `gaps.md` lists 急影 Lv1 rate 19 under monster 139

#### Scenario: A monster skill with no rate is listed, not written

- **GIVEN** the original monster 1 史萊姆 whose skill 爆氣 Lv2 has no rate
- **WHEN** the import runs
- **THEN** `monster-skills.json` has no row for `monster-1`, and `gaps.md` lists 爆氣 Lv2 under monster 1 with the reason that it has no rate

#### Scenario: A weapon's skill is imported

- **GIVEN** the original item 1823 爆氣火斧 with skill 2, which is 爆氣 Lv2, at rate 12
- **WHEN** the import runs
- **THEN** `item-1823` has weapon skill `skill-1`, level 2, rate 12, and `gaps.md` does not list that item's skill

#### Scenario: A description chance is multiplied into the rate

- **GIVEN** the original item 1828 神經系毒氣 with 鋒針陷阱 level 2, whose `Chance` is 18, at rate 12
- **WHEN** the import runs
- **THEN** `item-1828` has rate 2, and `gaps.md` records the conversion from rate 12 and `Chance` 18

#### Scenario: 假死 is imported

- **WHEN** the import runs
- **THEN** `skills.json` has 假死 with 7 levels, level 1 has `Chance` 40, 9 SP cost and a `Stun` effect with chance 2, `chanceByStat` intelligence 5 and 1 round, the job 刺客 can learn it, and `gaps.md` no longer lists 假死 as not imported

#### Scenario: Every imported monster's skills are written

- **WHEN** `JsonGameContent.Load` reads `backend/content` after the import
- **THEN** `monster-skills.json` has 568 rows, one for each of the 576 original rows on imported monsters except the 8 repeated ones

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

- **GIVEN** the original monster 426 魔王史萊姆 with a drop of the item 2277 that was not imported
- **WHEN** the import runs
- **THEN** `monster-426` is imported without that drop, and `gaps.md` lists the drop under monster 426

#### Scenario: A weapon with a skill that was not imported is listed

- **GIVEN** the original item 1834 翼龍獸矛 with the pet skill 戰意驅使 at rate 10
- **WHEN** the import runs
- **THEN** `item-1834` has no weapon skill, and `gaps.md` lists that field under item 1834

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
