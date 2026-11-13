## Purpose

The inn screen is the first redesigned facility page, opened from the town's market: the innkeeper Marsha in the lobby and a counter where the player buys room bonuses and adjusts the automatic rest thresholds. Feedback and errors are spoken by the innkeeper rather than shown as system messages.

## ADDED Requirements

### Requirement: Enter and leave the inn from the market

Clicking "旅館" in the town's market SHALL open the inn screen with a circular reveal of 0.55 seconds centred on the stage point (195 − 81k, 300k + 19), where k is the stage height / 844. The inn's back button "市集" SHALL close it with the reverse reveal toward the same point and SHALL show the town at the market segment with the character as last confirmed in the inn. The inn screen SHALL make no adventure requests.

#### Scenario: Reveal origin follows the stage height

- **WHEN** the inn opens on stages of different heights
- **THEN** the reveal is centred on the market's inn sign

##### Example: reveal origin

| Stage height | Origin |
| --- | --- |
| 624 | (135, 241) |
| 780 | (120, 296) |

#### Scenario: Back returns to the market

- **WHEN** the player books a room in the inn and presses "市集"
- **THEN** the town shows the market segment and the money after the booking

#### Scenario: No adventure requests in the inn

- **WHEN** the inn stays open for several polling intervals
- **THEN** no adventure advance request is made

### Requirement: Inn layout follows the stage height

The counter SHALL start at pt = 250 − (844 − H) × 0.4 for stage height H, the lobby SHALL be pt + 40 tall, the innkeeper portrait SHALL stay 420 tall at top 30 without scaling, and the room list or the automatic rest panel SHALL fill the counter's remaining height and scroll inside it; the page itself SHALL NOT scroll.

#### Scenario: Counter top for stage heights

- **WHEN** the inn is shown on stages of different heights
- **THEN** the counter starts at pt

##### Example: counter top

| Stage height | pt |
| --- | --- |
| 624 | 162 |
| 683 | 186 |
| 720 | 200 |
| 780 | 224 |

### Requirement: Current stay banner

The counter SHALL show the stay in effect as a banner with the room badge, the room name, the effect as `<kind name> ×<multiplier>` with two decimals, `剩 <remaining>` in the same remaining time format as the town's bonus row (`H:MM` from one hour up, `MM:SS` below one hour, counted from the stay's start when the local clock is earlier than it) and a remaining bar of (end − now) / (end − start), recalculated every second; when no stay is in effect it SHALL show `目前沒有住宿加成`. When the stay ends while the inn is open the banner SHALL change to `目前沒有住宿加成`.

#### Scenario: Banner for an active stay

- **WHEN** a stay in `apprentice-loft` (Experience ×1.10) runs from 08:00 to 16:00 and it is 08:01
- **THEN** the banner shows `學徒閣樓`, `經驗 ×1.10` and `剩 7:59`

#### Scenario: Banner counts seconds under an hour

- **WHEN** a stay has 10 minutes left and one second passes
- **THEN** the banner changes from `剩 10:00` to `剩 09:59`

### Requirement: Room cards

The "客房" tab SHALL list every room as a card with the room background, the room badge, the name, the effect, the stay length and the price with thousands separators; the room of the stay in effect SHALL carry the badge `入住中`. Room badges and backgrounds SHALL come from the content images `room-badges/<room id>` and `room-backgrounds/<room id>`, falling back to each category's placeholder. The stay length SHALL be written from `seconds`: whole hours as `H 小時`, less than an hour as `M 分`, otherwise `H 小時 M 分`, where M is the remaining minutes rounded.

#### Scenario: Stay length text

- **WHEN** stay lengths are formatted
- **THEN** they follow the hour and minute format

##### Example: stay length

| seconds | Text |
| --- | --- |
| 28800 | `8 小時` |
| 27000 | `7 小時 30 分` |
| 2700 | `45 分` |
| 86400 | `24 小時` |

### Requirement: Room detail and booking

Clicking a room card SHALL slide up a detail sheet with the badge, name, effect, cost and a seal button "入住"; clicking outside the sheet or its close button SHALL close it. When a different room is in effect the sheet SHALL show `會取代目前的<room name>（<effect>，剩 <remaining>）`; when the same room is in effect it SHALL show `正在住這間，剩 <remaining>`, both in that remaining time format. When the money is less than the cost, the cost SHALL be marked as insufficient and the seal SHALL be disabled; the seal SHALL also be disabled while a booking is in flight. A successful booking SHALL replace the banner with the booked stay, update the money, close the sheet and report the new character to the game screen.

#### Scenario: Booking succeeds

- **WHEN** a level 6 character with 1,000 money and no stay books `apprentice-loft`
- **THEN** the banner shows `學徒閣樓`, the money shows `400`, the sheet closes and the innkeeper says `學徒閣樓收拾好了，好好睡一覺吧！`

#### Scenario: Not enough money in the sheet

- **WHEN** a level 6 character with 500 money opens `apprentice-loft` (600)
- **THEN** the cost is marked insufficient and the seal "入住" is disabled

#### Scenario: Replacing another room

- **WHEN** a stay in `fighters-bunk` has 1:42 left and the player opens `apprentice-loft`
- **THEN** the sheet shows `會取代目前的鬥士通鋪（物攻 ×1.05，剩 1:42）`

### Requirement: Innkeeper lines

The innkeeper's speech bubble SHALL show one line chosen from the latest event, and SHALL return to the greeting at the next action (opening a room, stepping a threshold, retrying, switching tabs).

#### Scenario: Line for each situation

- **WHEN** the inn shows the innkeeper's line
- **THEN** the line matches the situation

##### Example: innkeeper lines

| Situation | Line |
| --- | --- |
| No event, no stay | 歡迎光臨暖爐亭！今晚想住哪一間？ |
| No event, a stay in effect | 房間還幫你留著呢。要換一間，還是續住？ |
| Booked `學徒閣樓` | 學徒閣樓收拾好了，好好睡一覺吧！ |
| Booking rejected with `notEnoughMoney` | 哎呀，這間的錢好像還差一點呢。 |
| Booking failed otherwise | 咦，登記簿沒寫上去……等會兒再來吧。 |
| Saving a threshold failed | 剛剛那個沒記下來，再調一次好嗎？ |
| Loading the inn failed | 哎呀，今天的房帳一時找不到…… |

### Requirement: Booking failures

When a booking is rejected with reason `notEnoughMoney`, the inn SHALL reload the character, show the reloaded money and keep the detail sheet open; when reloading fails the money SHALL stay as it was. When a booking fails for any other reason the detail sheet SHALL stay open with the seal enabled. No system message box SHALL be shown.

#### Scenario: Money spent elsewhere

- **WHEN** the inn shows 1,000 money, the stored character has 500, and booking `apprentice-loft` is rejected with `notEnoughMoney`
- **THEN** the money shows `500`, the sheet stays open with the seal disabled, and the innkeeper says `哎呀，這間的錢好像還差一點呢。`

#### Scenario: Connection failure

- **WHEN** booking fails with a network error
- **THEN** the sheet stays open with the seal enabled and the innkeeper says `咦，登記簿沒寫上去……等會兒再來吧。`

### Requirement: Automatic rest thresholds

The "自動住宿" tab SHALL show the HP and SP rest thresholds with a gauge, the value as `N%` or `略過` for −1, and − and ＋ buttons that step the value; ＋ SHALL be disabled at 100 and − SHALL be disabled at 略過. The displayed value SHALL change at once; `InnThresholdSaveDelayMilliseconds` after the last step the inn SHALL save the character's adventure settings with the two thresholds replaced, and leaving the inn SHALL save a pending change at once. When saving fails both thresholds SHALL return to the last saved values and the innkeeper SHALL say the threshold failure line.

#### Scenario: Stepping a threshold

- **WHEN** − or ＋ is pressed
- **THEN** the value steps to the neighbouring multiple of 5, with −1 for 略過

##### Example: threshold steps

| Value | ＋ | − |
| --- | --- | --- |
| −1 | 0 | disabled |
| 0 | 5 | −1 |
| 33 | 35 | 30 |
| 35 | 40 | 30 |
| 100 | disabled | 95 |

#### Scenario: Several steps save once

- **WHEN** the player presses HP ＋ three times within the save delay starting from 15
- **THEN** one settings update is sent with the HP threshold 30

#### Scenario: Saving fails

- **WHEN** the saved HP threshold is 15, the player steps it to 20 and saving fails
- **THEN** the HP threshold shows `15%` again and the innkeeper says `剛剛那個沒記下來，再調一次好嗎？`

### Requirement: Rest fee display

The "自動住宿" tab SHALL show the rest fee for the character's level: `免費` when the inn's `restFee` is 0, otherwise the fee with thousands separators followed by ` 起`, next to `Lv N`.

#### Scenario: Rest fee text

- **WHEN** the inn reports a rest fee
- **THEN** the text follows the fee

##### Example: rest fee text

| Level | restFee | Text |
| --- | --- | --- |
| 6 | 0 | 免費 |
| 120 | 1200 | 1,200 起 |

### Requirement: Inn load failure

When loading the inn fails, the counter SHALL show only a seal button `再試一次` without the banner, tabs or lists, and the innkeeper SHALL say the load failure line; pressing the seal SHALL load the inn again and show the rooms when it succeeds.

#### Scenario: Retry after a failed load

- **WHEN** loading the inn fails once and the player presses `再試一次`
- **THEN** the inn loads again and the six room cards are shown
