## MODIFIED Requirements

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
