# missions-screen Specification

## Purpose

Defines the mission board screen opened from the town's guild square: the cork notice board with a daily page and a regular page, the three mission slips in hand on the counter, the close-up mission sheet for accepting, abandoning and completing, Lise's feedback lines, and the loading, failure and empty states. It replaces the old tabbed screen's missions tab.

## Requirements

### Requirement: Enter and leave the missions screen

Clicking "任務板" at the town's guild square SHALL open the missions screen with a circular reveal from that sign's centre (0.55 seconds). The back button "公會前" SHALL close the screen with the reverse reveal to the same point and show the town at the guild square segment with the latest character. While the missions screen is shown, no request to `POST /api/adventure/advance` SHALL be sent.

#### Scenario: Open from the guild square

- **WHEN** the player clicks "任務板" at the guild square
- **THEN** the mission board opens and the old tabbed screen is not shown

#### Scenario: Back to the guild square

- **WHEN** the player presses "公會前" on the mission board
- **THEN** the town is shown at the guild square segment

#### Scenario: No adventure requests

- **WHEN** the player stays on the mission board and completes a mission
- **THEN** no request to `POST /api/adventure/advance` is sent

---
### Requirement: Notice board pages

The cork notice board SHALL have two pages, "每日委託" and "一般委託". Two title papers SHALL sit side by side at the board's top edge, "每日委託" on the left and "一般委託" on the right; the current page's paper SHALL be in front and the other page's paper behind it, dimmed and tilted. Clicking the paper behind SHALL switch to its page. A horizontal swipe of more than 40 px on the board SHALL also go to the next or previous page and SHALL NOT open a slip; there is no wrap-around. No text SHALL explain how to switch pages.

The paper behind SHALL show a red count when its page has missions the player can accept: for the daily page, the available daily missions (not completed today and not in hand); for the regular page, all available regular missions. A count of 0 SHALL NOT be shown, and the current page's paper SHALL NOT show a count.

Each time the missions screen is opened from the guild square, the board SHALL open on the daily page when the daily page lists at least one slip (available or completed today), and on the regular page otherwise. The page SHALL be chosen when the board first loads; while loading, the daily paper SHALL be in front and no count SHALL be shown.

Each slip on the board SHALL show the mission title without the "[每日]" prefix, the giver, "Lv N" and a kind label: "討伐" when the mission has kill targets, otherwise "寵物" when it asks for a pet, otherwise "交付".

#### Scenario: Swipe to the regular page

- **WHEN** the player swipes left by 60 px on the daily page
- **THEN** the regular page is shown and no mission sheet opens

#### Scenario: Click the paper behind

- **GIVEN** the daily page is shown
- **WHEN** the player clicks the "一般委託" paper
- **THEN** the regular page is shown and the "一般委託" paper is in front

#### Scenario: Opening page and counts

| Character | Board | Opens on | Count on the paper behind |
| --- | --- | --- | --- |
| Lv 20, no daily open | 7 available regular | regular | none (daily has 0) |
| Lv 150 | 3 daily available, 21 regular available | daily | 一般委託 21 |
| Lv 150, on the regular page | same | — | 每日委託 3 |
| Lv 150, every unlocked daily in hand | 21 regular available | regular | none |
| Lv 150, one daily completed today, the rest in hand | 21 regular available | daily | 一般委託 21 |

#### Scenario: Kind label and title

| Mission | Board slip |
| --- | --- |
| daily `[每日]討伐任務` | title 討伐任務, kind 討伐 |
| `mission-1` (delivers 蜘蛛絲) | title 製作漁網, kind 交付 |
| `mission-16` (asks for a pet) | kind 寵物 |

---
### Requirement: Daily page

The daily page SHALL list, in a 3-column grid without scrolling, every daily mission that is available or completed in the current period, ordered by level and then content order; daily missions in hand SHALL NOT be listed. A daily mission completed today SHALL stay in its place, faded, with a "完" stamp, and clicking it SHALL open its sheet in the completed view. A paper at the top right SHALL show the hours until the next reset as "N 小時後重置", where N is the remaining time rounded up to whole hours with a minimum of 1; it SHALL NOT be shown when the page lists no mission.

#### Scenario: Completed daily stays in place

- **GIVEN** a level-150 character with dailies of levels 50, 50, 150, 150 unlocked, one of the level-50 ones completed today and one level-150 one in hand
- **WHEN** the daily page is shown
- **THEN** it lists 3 slips in level order, the completed one faded with "完"

##### Example: Reset hours

| Now (Taipei) | Text |
| --- | --- |
| 09:00 | 24 小時後重置 |
| 08:10 | 1 小時後重置 |
| 21:30 | 12 小時後重置 |

---
### Requirement: Regular page and the completed pile

The regular page SHALL list every available regular mission in the board order and SHALL scroll vertically inside the cork when the slips do not fit. When the character has completed regular missions, a pile at the page's top right SHALL show their count with a "完" stamp; clicking it SHALL open the "完成的委託" list (title, giver, Lv; newest first, scrolling), and clicking an entry SHALL open that mission's sheet in the completed view, whose close button returns to the list.

#### Scenario: Many regular missions scroll

- **GIVEN** the board has 21 available regular missions
- **WHEN** the regular page is shown
- **THEN** all 21 slips are in the page and the page scrolls inside the cork

#### Scenario: Completed pile

- **GIVEN** the character has completed 30 regular missions
- **WHEN** the player opens the pile and then the first entry
- **THEN** the list shows 30 entries and the sheet shows the newest completed mission in the completed view; closing it shows the list again

---
### Requirement: Missions in hand

The counter SHALL show one slot per `activeLimit` from the board (3 today; 3 while the board is still loading) and a "進行中 N / activeLimit" paper. Each active mission SHALL be a slip in hand, in acceptance order, with its title (a red dot before daily missions), its first objective line and that objective's progress, "＋N 項" when there are more objectives, and a progress bar. Empty slots SHALL show an empty frame. When every objective is met the slip SHALL be stamped "可交" for a mission with a delivery or a pet, otherwise "達成"; a mission that only lacks its required skill level SHALL NOT be stamped. Clicking a slip SHALL open its sheet.

Objective progress SHALL be "progress / count" for a kill target, "owned / quantity" (owned capped at quantity) for a delivery item, "攜帶中" or "未攜帶" for a pet, and "目前 LvN" or "未學會" for a required skill. The progress bar SHALL be the sum of met amounts over the sum of needed amounts, counting each kill and delivery by its number and the pet and the skill as 1 each.

#### Scenario: Hand follows the server limit

- **GIVEN** a board with `activeLimit` 4 and 3 missions in hand
- **WHEN** the hand is shown
- **THEN** the counter has one empty slot, the paper reads "進行中 3 / 4", and an available mission's "接取" seal is enabled

#### Scenario: Several objectives

- **GIVEN** an active mission delivering 4 kinds of items, owning 2 / 3 of the first
- **WHEN** the hand is shown
- **THEN** its slip shows the first delivery with "2 / 3" and "＋3 項"

#### Scenario: Only the skill is missing

- **GIVEN** an active mission whose kills and deliveries are met but whose required skill is learned at Lv2 of Lv3
- **WHEN** the hand is shown
- **THEN** the slip has no stamp and the bar is below full

##### Example: Progress bar

| Objectives | Ratio |
| --- | --- |
| kill 3/3, kill 1/3 | 4/6 |
| deliver 2/3, pet not carried | 2/4 |
| kill 5/5, skill not met | 5/6 |

---
### Requirement: Mission sheet

Clicking a slip SHALL open the mission sheet over the board. The sheet SHALL show the title (without "[每日]"), "委託者　giver", the description, and for a mission not yet accepted its conditions joined by "・": "Lv N", "限 job", "限 男性／女性", "限 (birth)出身", "需先完成「title」". It SHALL list every objective: "擊敗 monster" with its place and "progress / count" (or "×count" before accepting), "交付 item" with the item's category placeholder and "owned / quantity" (or "×quantity"), "交付寵物 species" with "需攜帶" before accepting and "攜帶中／未攜帶" after, and "技能 skill LvN" with "目前 LvN／未學會"; unmet objectives of an accepted mission SHALL be marked. It SHALL list the rewards: "經驗 N", money with the coin icon, "item ×N" with the category placeholder, "技能 skill LvN", "寵物 species LvN". A daily mission's sheet SHALL show "每日委託・N 小時後重置". The content SHALL scroll inside the sheet while Lise's line and the action row stay fixed at the bottom. The close button SHALL return to the board (or to the completed list when opened from it).

#### Scenario: Sheet of an available mission

- **WHEN** the player opens `mission-1` from the regular page
- **THEN** the sheet shows 委託者　giver, "Lv 20", "交付 蜘蛛絲 ×1" with the material placeholder, "經驗 3,800" and "白色布料 ×1" with the material placeholder

#### Scenario: Sheet of an active mission with an unmet skill

- **GIVEN** `mission-318` is active and `skill-414` is learned at Lv1
- **WHEN** the player opens its slip
- **THEN** the sheet shows "技能 … Lv2" with "目前 Lv1", marked as unmet

---
### Requirement: Accept, abandon and complete from the sheet

The sheet of an available mission SHALL have a red seal "接取"; with `activeLimit` missions in hand the seal SHALL be disabled with "手邊已有 (activeLimit) 張" beside it. The sheet of an active mission SHALL have "放棄" and a red seal "交付" (when the mission has a delivery or a pet) or "完成"; the seal SHALL be disabled until the mission is ready. "放棄" SHALL ask again with "確定放棄" and "取消". While a request is being sent the seal SHALL look pressed, and the seal, "放棄" and the close button SHALL be disabled.

After a successful accept, abandon or complete the sheet SHALL stay open and show the mission's new state from the returned board. After a completion the sheet SHALL show a "完" stamp, the giver's completion message when there is one, and "這次拿到": "獲得經驗 N" (with "、金錢 N" when money is given), "獲得 item ×N", "學會了 skill LvN" or "已經學會(skill)，這次沒有學到" when `skillGranted` is false, "獲得寵物 species LvN", and "升到 LvN" with the new character level when levels were gained. The returned character SHALL replace the screen's character. A completed mission opened from the daily page or the completed list SHALL show the "完" stamp, the giver's completion message when there is one, and the mission's rewards.

#### Scenario: Accept keeps the sheet open

- **WHEN** the player accepts `mission-1` from its sheet
- **THEN** the sheet stays open with "放棄" and a disabled "交付" seal, and the hand shows the mission

#### Scenario: Hand is full

- **GIVEN** 3 missions in hand
- **WHEN** the player opens an available mission
- **THEN** the "接取" seal is disabled with "手邊已有 3 張"

#### Scenario: Completing with a level up

- **GIVEN** a ready mission whose reward raises the character from Lv 49 to Lv 50
- **WHEN** the player presses "完成"
- **THEN** the sheet shows the "完" stamp, "獲得經驗 …" and "升到 Lv50", and the slip leaves the hand

#### Scenario: Completion without a message

- **WHEN** the player completes `mission-3`, whose completion message is empty
- **THEN** the sheet shows the rewards without a giver's line

---
### Requirement: Lise speaks feedback and errors

Feedback and errors on the sheet SHALL be spoken by Lise above the action row, with her round portrait and one line; failure lines SHALL use the failure colour. The line SHALL be cleared by the next action (opening another sheet, closing the sheet, pressing the seal or "放棄"). No toast SHALL be shown. After a failed request the screen SHALL reload the mission board and keep the sheet open.

| Event | Failure | Lise says |
| --- | --- | --- |
| accepted | no | 「(title)」就拜託您了，路上小心！ |
| abandoned | no | 好的，這張先放回佈告欄囉。 |
| `ITEMS_MISSING` | yes | 交付的東西好像還差一點呢…… |
| `OBJECTIVE_INCOMPLETE` | yes | 討伐的數量好像還差一點呢…… |
| `PET_MISSING` | yes | 咦，您身邊沒有帶著(pet)呢…… |
| `SKILL_TOO_LOW` | yes | (skill)要練到 LvN 才行喔。 |
| `BAG_FULL` | yes | 哎呀，(category)的格子已經滿了…… |
| `PETS_FULL` | yes | 獸欄已經住滿 3 隻了，新夥伴沒地方住呢…… |
| `TOO_MANY` | yes | 您手邊已經有 (activeLimit) 張委託了喔。 |
| `MISSION_NOT_ACTIVE` | yes | 這張委託已經不在您手上了呢…… |
| any other reason, or no response | yes | 咦，剛剛沒送出去……再試一次吧？ |

(title) is the title without "[每日]"; (pet) and (skill) are the mission's pet species and required skill names and N the required level; (category) is the category label of the reward items when they all share one category, otherwise 背包. When a request fails with several reasons, Lise SHALL speak the line of the first reason in the table order.

#### Scenario: Bag full names the category

- **GIVEN** a ready mission whose only reward item is a material and the bag's material slots are full
- **WHEN** the player completes it
- **THEN** Lise says "哎呀，材料的格子已經滿了……" in the failure colour and the sheet stays open

#### Scenario: Network failure

- **WHEN** accepting a mission gets no response
- **THEN** Lise says "咦，剛剛沒送出去……再試一次吧？" and the board is reloaded

---
### Requirement: Loading, load failure and empty boards

While the board is loading, the cork SHALL show 6 blank slips that cannot be clicked, the hand SHALL show empty slots, the "進行中" count SHALL be blank, and the reset paper and the completed pile SHALL NOT be shown. When loading fails, the middle of the cork SHALL show Lise saying "哎呀，委託單一時找不到了……" and a red seal "再試一次" that reloads the board. A page with no slip SHALL show only pins, with no text; when the character's level is below `firstDailyLevel`, the daily page SHALL show a small paper "Lv N" with N = `firstDailyLevel`.

#### Scenario: Retry after a failure

- **GIVEN** loading the board failed
- **WHEN** the player presses "再試一次" and the board loads
- **THEN** the slips and the hand are shown

#### Scenario: Daily missions not open yet

- **GIVEN** a level-20 character and `firstDailyLevel` 50
- **WHEN** the daily page is shown
- **THEN** the cork shows a "Lv 50" paper and no slip

---
### Requirement: Mission board layout follows the stage height

For stage height H (624 to 780) and k = H / 844, the screen SHALL lay out as follows, rounded to whole pixels: hand slip height hh = 128 + (H − 624) × 0.256; hand bottom margin 8 + (H − 624) × 0.051; hand top = H − margin − hh; board top 46 and board height = hand top − 94; board slip row height min(112, (board height − 140) / 3); reveal centre x = max(195 + 104k, 390 − 113k), y = 540k + 19.

#### Scenario: Layout at the extremes

| H | Board height | Row height | Hand top | Hand height | Hand margin | Reveal centre |
| --- | --- | --- | --- | --- | --- | --- |
| 624 | 394 | 85 | 488 | 128 | 8 | (306, 418) |
| 683 | 435 | 98 | 529 | 143 | 11 | (299, 456) |
| 720 | 460 | 107 | 554 | 153 | 13 | (294, 480) |
| 780 | 502 | 112 | 596 | 168 | 16 | (291, 518) |
