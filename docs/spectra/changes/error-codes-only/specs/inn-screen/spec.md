## MODIFIED Requirements

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
| Booking rejected with `NOT_ENOUGH_MONEY` | 哎呀，這間的錢好像還差一點呢。 |
| Booking failed otherwise | 咦，登記簿沒寫上去……等會兒再來吧。 |
| Saving a threshold failed | 剛剛那個沒記下來，再調一次好嗎？ |
| Loading the inn failed | 哎呀，今天的房帳一時找不到…… |

---

### Requirement: Booking failures

When a booking is rejected with reason `NOT_ENOUGH_MONEY`, the inn SHALL reload the character, show the reloaded money and keep the detail sheet open; when reloading fails the money SHALL stay as it was. When a booking fails for any other reason the detail sheet SHALL stay open with the seal enabled. No system message box SHALL be shown.

#### Scenario: Money spent elsewhere

- **WHEN** the inn shows 1,000 money, the stored character has 500, and booking `apprentice-loft` is rejected with `NOT_ENOUGH_MONEY`
- **THEN** the money shows `500`, the sheet stays open with the seal disabled, and the innkeeper says `哎呀，這間的錢好像還差一點呢。`

#### Scenario: Connection failure

- **WHEN** booking fails with a network error
- **THEN** the sheet stays open with the seal enabled and the innkeeper says `咦，登記簿沒寫上去……等會兒再來吧。`

---
