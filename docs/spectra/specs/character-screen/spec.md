# character-screen Specification

## Purpose

The character screen is the player's equipment room: the current job's portrait, the adventurer registration card, and a journal with an abilities page and a skills page. It shows the character's profile, stats and active bonuses, lets the player sign out, and lets the player learn and upgrade skills with the current job's or a previous job's skill points.

## Requirements

### Requirement: Enter and leave the character screen

Pressing "角色" at the town gate SHALL open the character screen with a 0.55-second circular reveal centred on the stage point (311, 193) at every stage height, the same reveal as the inn, the companions, the honour hall and the mission board. The "城鎮" button at the top left SHALL close it with the reverse reveal to the same point and show the town at the gate segment with the latest character. The character screen SHALL open on the abilities page. While the character screen is shown, the client SHALL NOT request `POST /api/adventure/advance`.

#### Scenario: Open from the gate

- **WHEN** the player presses "角色" at the gate
- **THEN** the character screen opens on the abilities page and the old tabbed screen is not shown

#### Scenario: Back to the gate

- **WHEN** the player presses "城鎮" after learning a skill
- **THEN** the town is shown at the gate segment with the money left after learning

#### Scenario: No adventure requests

- **WHEN** the character screen is open
- **THEN** no `POST /api/adventure/advance` request is sent

---
### Requirement: Loading and load failure

Each time the character screen opens, it SHALL request `GET /api/characters/me`, `GET /api/accounts/me`, `GET /api/characters/me/buffs`, `GET /api/inn`, `GET /api/skills` and `GET /api/public/settings`. While any of them is loading, the registration card SHALL keep the place of every name and number as a faint block with empty bars, the portrait SHALL not be shown, the wallet number SHALL be a faint block, and the journal SHALL show blank lines. When any of them fails, the journal SHALL show the line `登記證的資料讀不到。` and a "再試一次" red seal; pressing the seal SHALL request all six again.

#### Scenario: Loading

- **WHEN** the character screen opens and the requests have not answered
- **THEN** the card shows faint blocks instead of text and the journal shows blank lines

#### Scenario: Retry after a failure

- **WHEN** `GET /api/skills` fails and the player presses "再試一次"
- **THEN** the six requests are sent again and the screen shows the loaded character when they succeed

---
### Requirement: Registration card and portrait

The registration card SHALL show the character's name; the current job's icon, name and `Lv N`; a "轉職" button; `熟練度 N%` with `value / 3,500` and a bar for the current job's proficiency, where N is the proficiency percent rounded down and capped at 100; the sex, element and birth names from the public settings; `HP current / maximum`, `SP current / maximum` and `EXP N%` (experience divided by the next level's experience, rounded down), each with a bar. Numbers SHALL use thousands separators. The portrait SHALL be the current job's portrait for the character's sex (the female portrait for sex 2, the male portrait otherwise). When the content manifest has no portrait for that job and sex, the portrait area SHALL show the job icon and `立繪待補` instead. In this change, the "轉職" button SHALL open the old tabbed screen's skills tab, where the job change remains.

#### Scenario: Card values

- **GIVEN** a level 162 戰士 named 收尾者 with 2,450 戰士 proficiency, 6,212 of 9,640 HP, 980 of 1,205 SP and 42,000 of 100,000 experience
- **WHEN** the character screen has loaded
- **THEN** the card shows 收尾者, 戰士, `Lv 162`, `熟練度 70%`, `2,450 / 3,500`, `HP 6,212 / 9,640`, `SP 980 / 1,205` and `EXP 42%`

#### Scenario: Proficiency at the cap

- **WHEN** the current job's proficiency is 3,500
- **THEN** the card shows `熟練度 100%`

#### Scenario: Portrait by sex

- **WHEN** a female 術士 opens the character screen
- **THEN** the portrait is the 術士 female portrait

#### Scenario: Job without a portrait

- **WHEN** a 狂戰士 (`job-10`) opens the character screen and the manifest has no 狂戰士 portrait
- **THEN** the portrait area shows the 狂戰士 icon and `立繪待補`

#### Scenario: Job change button

- **WHEN** the player presses "轉職" on the card
- **THEN** the old tabbed screen opens on the skills tab

---
### Requirement: Wallet

The wallet at the left of the desk edge SHALL show the character's money with thousands separators on both journal pages, and SHALL show the new amount as soon as a learning response arrives.

#### Scenario: Money after learning

- **WHEN** learning a skill that costs 2,000 money succeeds with 5,000 money
- **THEN** the wallet shows `3,000`

---
### Requirement: Abilities page

The abilities page SHALL show, in order:

- **能力**: the seven base stats (力量, 智力, 生命, 體質, 敏捷, 魅力, 信仰) as the base value, with the equipment bonus (effective value minus base value) as a signed `+N` or `-N` when it is not zero.
- **戰鬥數值**: 物攻, 魔攻, 物防, 魔防, HP 上限 and SP 上限.
- **密藥與住宿**: the active lodging and elixirs, in the same order, names, effect lines and remaining-time format as the town's bonus list. Each remaining time counts down every second and is marked red when less than 10 minutes remain. When nothing is active, the section shows `沒有生效中的密藥或住宿`. When a bonus's end time passes, it disappears without a new request.
- **Sign-out row**: the account's username and a "登出" button.

#### Scenario: Stat with an equipment bonus

- **WHEN** the base strength is 412 and the effective strength is 430
- **THEN** the 力量 row shows `412` with `+18`

#### Scenario: Stat without a bonus

- **WHEN** the base intelligence equals the effective intelligence
- **THEN** the 智力 row shows no bonus

#### Scenario: Bonus list

- **WHEN** the character has an active lodging and an Attack elixir
- **THEN** the lodging row is first, then the elixir row, each with the town's remaining-time text

#### Scenario: Nothing active

- **WHEN** no lodging and no elixir is active
- **THEN** the section shows `沒有生效中的密藥或住宿`

---
### Requirement: Sign out from the abilities page

Pressing "登出" SHALL open a confirmation with `要登出 ＜username＞ 嗎？`, "取消" and "登出". "取消" or a press outside the confirmation SHALL close it. Confirming SHALL clear the stored access token without a server request and show the title screen.

#### Scenario: Confirm sign out

- **WHEN** the player presses "登出" and then confirms with "登出"
- **THEN** the stored access token is removed and the title screen is shown

#### Scenario: Cancel sign out

- **WHEN** the player presses "登出" and then "取消"
- **THEN** the confirmation closes and the abilities page stays

---
### Requirement: Skills page job switcher

The skills page SHALL show one job at a time from the skill overview's `jobs`, starting with the current job each time the page opens. A switcher at the top SHALL show the shown job's icon and name, `現職` when it is the current job, `Class R・rank・已學 L / T 招` for the current job or `Class R・rank・已學 L 招` for a held job (R is the Roman class number, rank is 見習, 正式, 精英, 大師 or 傳奇 for classes I to V, L the learned skills and T the job's skills), and `技能點` with that job's skill points. Pressing the switcher SHALL open the list of held jobs grouped by class (`CLASS R・rank` with the job count), each job showing `現職` or `技能點 N`. Choosing a job SHALL close the list and show that job's skills. The skills page SHALL NOT show the word 熟練度.

#### Scenario: Switcher of the current job

- **GIVEN** a 戰士 with 2,587 skill points who has learned 3 of the 戰士's 4 skills
- **WHEN** the skills page opens
- **THEN** the switcher shows 戰士, `現職`, `Class I・見習・已學 3 / 4 招` and `技能點 2,587`

#### Scenario: Choosing a previous job

- **WHEN** the player opens the switcher and chooses 術士
- **THEN** the page lists only the 術士 skills the character has learned

---
### Requirement: Skill cards

Each skill of the shown job SHALL be a card with the skill icon (active or passive frame), the name, `Lv N/M` or `未習得` when not learned, one diamond per level up to the maximum with the learned ones lit, `主動` or `被動`, and the description. When the description is empty, the card SHALL show `（說明待補）`. Pressing a card's header SHALL expand it and collapse the card expanded before. An expanded card SHALL show a column for the current level (`尚未學習` when not learned) and a column for the next level, each listing the detail rows named 發動, 消耗, 效果 and 冷卻, with the next level's values that differ from the current level in green. Its footer SHALL show:

- At the maximum level: `已達最高等級` and a `精通` seal.
- Otherwise: `升級需要 X 金錢 Y 技能點` (or `學習需要` when not learned), with an amount that is not enough in red, and a red seal button labelled `升級` (or `學習` when not learned).
- When `canLearn` is false: the seal is disabled and, on the next line, the unmet reasons joined by `・` — `前置技能：＜name＞ Lv N`, `技能點不足` and `金錢不足`, in that order.

#### Scenario: Disabled reasons

- **WHEN** an expanded card's prerequisite 防禦心得 Lv 3 is not met and its skill points are not enough
- **THEN** the seal is disabled and the line shows `前置技能：防禦心得 Lv 3・技能點不足`

#### Scenario: Cost line

- **WHEN** a card's next level costs 27,000 money and 2,587 skill points, and the money is not enough
- **THEN** the footer shows `升級需要 27,000 金錢 2,587 技能點` with `27,000 金錢` in red

#### Scenario: Maximum level

- **WHEN** a skill is at its maximum level
- **THEN** its footer shows `已達最高等級` and the `精通` seal and no button

---
### Requirement: Learning from the skills page

Pressing an enabled seal SHALL send `POST /api/skills/learn` with the skill id and, when the shown job is not the current job, the shown job's id, with the shared waiting ink dots (see the `action-feedback` capability). On success the screen SHALL replace the skill overview and the character with the response, stay on the same job with the same card expanded, update the wallet, and show a success popup with one "確定" button that a press outside also closes:

- **升級成功**: the skill icon (with a `精通` seal when the new level is the maximum), the name, `Lv old → Lv new / M`, the diamonds, and the new level's detail rows, with the rows that differ from the previous level in green.
- **習得成功** (from level 0): the skill icon, the name, `Lv 1 / M`, the diamonds and level 1's detail rows.

On failure the screen SHALL keep its state and show the shared failure popup with `連線失敗`, or the text of the first known reason: `金錢不足` for `NOT_ENOUGH_MONEY`, `技能點不足` for `NOT_ENOUGH_PROFICIENCY`, `前置技能：＜name＞ Lv N` for `PREREQUISITE`, `已達最高等級` for `MAX_LEVEL`, `不是這個職業的技能` for `WRONG_JOB` and `尚未學會這個技能` for `NOT_LEARNED`.

#### Scenario: Upgrade with the current job

- **WHEN** the player upgrades 爆氣 from Lv 6 with the current job and the request succeeds
- **THEN** the card shows `Lv 7/10`, the switcher shows the remaining skill points, and the `升級成功` popup shows `Lv 6 → Lv 7 / 10`

#### Scenario: Upgrade with a previous job

- **WHEN** the shown job is 術士 and the player upgrades 魔力增幅
- **THEN** the request carries `jobId` `job-7` and the 術士's skill points in the switcher decrease

#### Scenario: Learning a new skill

- **WHEN** the player learns 防禦心得 from level 0 and the request succeeds
- **THEN** the `習得成功` popup shows `Lv 1 / 10`

#### Scenario: Server rejection

- **WHEN** learning fails with `NOT_ENOUGH_MONEY`
- **THEN** the failure popup shows `金錢不足` and the card is unchanged

---
### Requirement: Character screen layout follows the stage height

With stage height H from 624 to 780, the screen SHALL use the card scale cs = 0.88 + (H − 624) / 156 × 0.12 and the desk edge dt = 16 + 329 × cs + 7. The registration card SHALL keep its top right corner at (right 8, top 16) and scale by cs. The portrait SHALL be 336 × cs tall with its top at dt + 6 − 336 × cs, and the centre of its feet SHALL be at x = 110.5 × cs − 10 after shifting the image by the per-portrait feet offset times the portrait height. The journal tabs SHALL be at dt + 4 and the journal SHALL run from dt + 40 to the bottom of the stage, scrolling inside. Text on the card SHALL stay at least 11 px at cs 0.88.

#### Scenario: Layout at the extremes

- **WHEN** the layout is computed for the shortest and the tallest stage
- **THEN** it matches the table

##### Example: layout values

| Stage height | cs | dt | Portrait top | Feet centre x | Journal top |
| --- | --- | --- | --- | --- | --- |
| 624 | 0.88 | 312.52 | 22.84 | 87.24 | 352.52 |
| 780 | 1 | 352 | 22 | 100.5 | 392 |

#### Scenario: Feet offset

- **WHEN** the 術士 male portrait (feet offset +0.035) is 336 px tall
- **THEN** the image shifts left by 11.76 px so its feet stay at the feet centre
