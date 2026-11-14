## Purpose

The honour screen is the redesigned honour hall, opened from the town's guild square segment: a counter where the clerk Lise greets the player over a case of five medals, a display rack of hanging tags for the rewards one medal buys, and a parchment sheet for the details and the exchange. Feedback and errors are spoken by Lise rather than shown as system messages.

## ADDED Requirements

### Requirement: Enter and leave the honour screen

Clicking "勳章所" at the town's guild square segment SHALL open the honour counter with a circular reveal of 0.55 seconds centred on that sign. The counter's back button "公會前" SHALL close it toward the same point and show the town at the guild square segment with the latest character. The rack's back button "櫃檯" SHALL raise the rack and bring Lise back to the counter. The honour screen SHALL make no adventure requests.

#### Scenario: Back from the rack and the counter

- **WHEN** the player opens "勳章所", clicks a medal, presses "櫃檯", then presses "公會前"
- **THEN** the counter is shown after "櫃檯" and the town is shown at the guild square segment after "公會前"

#### Scenario: No adventure requests

- **WHEN** the player stays on the honour screen
- **THEN** no `POST /api/adventure/advance` request is sent

### Requirement: Honour layout follows the stage height

With stage height H and k = H / 844, the reveal centre SHALL be (max(390 − 139k, 195 + 78k), 256k + 19); the medal case SHALL be 330 + (H − 624) × 0.282 wide, 0.47 of its width tall, centred, and 20 + (H − 624) × 0.051 above the stage bottom; the rack SHALL start 50 from the top and its height SHALL be the case's top edge minus 112. Values SHALL be rounded to whole pixels. The rack list and the parchment's upper part SHALL scroll inside their areas, and the page SHALL NOT scroll.

#### Scenario: Layout at the two ends

- **WHEN** the stage height is 624 or 780
- **THEN** the layout follows the table below

##### Example: layout by height

| H | Reveal centre | Case | Case bottom gap | Case top | Rack height |
| --- | --- | --- | --- | --- | --- |
| 624 | (287, 208) | 330 × 155 | 20 | 449 | 337 |
| 683 | (278, 226) | 347 × 163 | 23 | 497 | 385 |
| 720 | (271, 237) | 357 × 168 | 25 | 527 | 415 |
| 780 | (267, 256) | 374 × 176 | 28 | 576 | 464 |

### Requirement: Honour counter

The counter SHALL show Lise with a speech bubble and the medal case with the five medals in content order, each with its owned count. Clicking a medal SHALL send Lise away and lower the rack for that medal. When a contract exists, a note `僱用中` with the contract name SHALL be pinned above the case. Lise's line SHALL follow the clerk lines table in "Lise speaks feedback and errors".

#### Scenario: Counter with medals and a contract

- **WHEN** the player with 14 continent, 6 kingdom, 3 rainforest, 0 royal city and 2 silver medals and a contract named 葛雷 opens the honour screen
- **THEN** the case shows 14, 6, 3, 0, 2, the note reads `僱用中 葛雷`, and Lise says `歡迎來到勳章所！今天想兌換些什麼呢？`

#### Scenario: Counter without medals

- **WHEN** the player owns no medals
- **THEN** Lise says `啊，您還沒有勳章呢……有了再來找我喔！` and the medals can still be clicked

### Requirement: Display rack

The rack SHALL show a brass plate with the medal's name and a tag for every exchange whose cost includes that medal, so an exchange paid with several medals appears on each of their racks. Tags SHALL be grouped under section plates in the order 傭兵, 武器, 頭部, 身體, 手部, 腳部, 飾品, 鑰匙, 道具, 魔石, 精鍊石, 材料, 勳章, with empty sections hidden, and within a section sorted by the quantity of this medal needed, ascending, then by content order. A tag SHALL show the reward's picture, its name in at most two lines, `×N` when the quantity is above one, and the cost with each medal's picture and quantity, a short quantity in red. A tag whose exchange is not affordable, or a mercenary tag while a contract exists, SHALL be dimmed but still open its sheet.

#### Scenario: Rainforest rack

- **WHEN** the player clicks the rainforest medal
- **THEN** the rack shows 武器 9, 頭部 15, 鑰匙 2, 道具 1 and 材料 1 tags, and the weapons start with 白陽短劍 (8), 偏斜針 (9), 流影套索 (10)

##### Example: silver rack order

| Position | Tag | Silver needed |
| --- | --- | --- |
| 1 | 90000HP回復劑 ×10 | 1 |
| 2 | 大魔石寶箱 | 1 |
| 3 | 魔法捕捉器 ×5 | 2 |
| 4 | 1500SP藥水 ×10 | 2 |
| 12 | 劣質打孔石 ×3 (材料) | 1 |

### Requirement: Exchange sheet

Clicking a tag SHALL open a parchment sheet with the reward's picture with `×N`, its name, `<category>・綁定` or the category alone, the effect for consumables and keys, the stat bonus, the equipment requirements coloured green when met and red when not (colour only, the exchange is not blocked), `孔 n・精鍊上限 +n` for equipment, and for a mercenary the fights, money, drop and experience percents and death cost. The bottom of the sheet SHALL stay fixed and show, for each medal in the cost, `持有 N／需要 M` with a short N in red, and the red seal `兌換`. The seal SHALL be disabled with `勳章不足` beside it when the exchange is not affordable, and with `僱用中：<name>` for a mercenary while a contract exists. Closing the sheet or clicking another tag SHALL replace or close it.

#### Scenario: Equipment with an unmet requirement

- **WHEN** the player with base strength 300 opens 刺心者 on the continent rack
- **THEN** the sheet shows `武器・綁定`, the requirement `力量 435` in red, `孔 2・精鍊上限 +4`, and `持有 14／需要 9`

#### Scenario: Effects of consumables and keys

- **WHEN** the player opens 90000HP回復劑 or 靜止鑰匙
- **THEN** the effect reads a HP recovery of 90,000 for the potion and entry to 靜止之城 for 24 小時 for the key

##### Example: effect text

| Item kind | Effect |
| --- | --- |
| Supply with supply HP 90000 | 補給欄自動使用：HP 回復 90,000 |
| Key to 靜止之城 for 86400 seconds | 開啟「靜止之城」24 小時 |
| Elixir attack ×1.05 for 7200 seconds | 物攻 ×1.05，2 小時 |
| Capture with amount 5 | 捕捉寵物用（捕捉力 5） |
| Stone box with amount 1 | 打開隨機獲得魔石 ×1 |
| Refine box with amount 1 | 打開隨機獲得精鍊石 ×1 |
| Pet egg | 孵出一隻隨機寵物 |

### Requirement: Exchanging from the sheet

Pressing an enabled seal SHALL send the exchange. While it is in flight the seal SHALL look pressed with `兌換` blinking, and the close button, the tags and the medal case SHALL NOT respond. On success the sheet SHALL stay open on the same exchange and the whole screen SHALL use the returned listing, so owned counts, dimming and the seal's state are recalculated, and the character SHALL be reloaded. On failure the screen SHALL reload the honour listing and keep the sheet open.

#### Scenario: Exchanging twice in a row

- **WHEN** the player with 2 silver medals exchanges 90000HP回復劑 ×10 and the sheet stays open
- **THEN** the sheet shows `持有 1／需要 1`, the seal is still enabled, and a second exchange leaves `持有 0／需要 1` with the seal disabled

### Requirement: Lise speaks feedback and errors

Feedback and errors SHALL be spoken by Lise and SHALL NOT appear as system messages or toasts. At the counter she SHALL speak in the bubble; while the rack is down she SHALL speak inside the sheet above the seal row with her round portrait and one line, the frame reddish for failures. A line inside the sheet SHALL clear on the next action (another tag, closing the sheet, another exchange). Returning to the counter after at least one exchange SHALL make her say the thanks line; returning without one SHALL make her say the browsing line.

#### Scenario: Bag full names the category

- **WHEN** an exchange of 破山斧 is refused with `bagFull`
- **THEN** Lise says `哎呀，武器的格子已經滿了……` in the sheet with the failure colour

##### Example: clerk lines

| Event | Failure | Lise says |
| --- | --- | --- |
| counter with any medal | no | 歡迎來到勳章所！今天想兌換些什麼呢？ |
| counter with no medals | no | 啊，您還沒有勳章呢……有了再來找我喔！ |
| back to the counter without exchanging | no | 慢慢挑沒關係，陳列板不會跑掉的～ |
| back to the counter after exchanging | no | 兌換好了！祝您冒險一路順風。 |
| loading | no | 歡迎來到勳章所！我翻一下您的帳冊喔…… |
| load failure | yes | 哎呀，帳冊一時翻不到了…… |
| item exchanged | no | 換好了！已經幫您放進背包囉。 |
| mercenary exchanged | no | C級傭兵的契約簽好囉，祝您順利！ |
| `bagFull` for 90000HP回復劑 | yes | 哎呀，道具的格子已經滿了…… |
| `mercenaryHired` | yes | 您已經有一位傭兵了，一次只能一位喔。 |
| `medalsMissing` | yes | 勳章好像還差一點呢…… |
| any other reason or a network failure | yes | 咦，剛剛沒送出去……再試一次吧？ |

### Requirement: Honour loading and load failure

While the honour listing is loading, Lise SHALL say the loading line, the medal counts SHALL keep their place without numbers, and the medals SHALL NOT respond. When loading fails, Lise SHALL say the load failure line and a red seal `再試一次` SHALL appear below the bubble; pressing it SHALL load the listing again.

#### Scenario: Retry after a load failure

- **WHEN** the honour listing fails to load and the player presses `再試一次` after the server recovers
- **THEN** the medal counts appear, the retry seal is gone and Lise greets the player

### Requirement: Reward pictures

A tag and a sheet SHALL show the item's content image `items/<item id>`; when the item has no content image or it fails to load, they SHALL show the line placeholder for the item's category (one of the twelve item categories). A mercenary reward SHALL show the plumed helmet picture. Medals SHALL use their item content images.

#### Scenario: Item without a content image

- **WHEN** the rack shows 巨石槌, which has no content image
- **THEN** its tag shows the weapon placeholder rather than the generic item placeholder

##### Example: pictures by reward

| Reward | Content image in the manifest | Picture shown |
| --- | --- | --- |
| 巨石槌 (Weapon) | no | weapon placeholder |
| 靜止鑰匙 (Key) | no | key placeholder |
| 劣質打孔石 (Material) | no | material placeholder |
| C級傭兵 | — | plumed helmet |
| 大陸勳章 in the cost | `items/item-1304` | the medal image |
